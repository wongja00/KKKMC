using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 템플릿 플레이어(또는 캐릭터) 프리팹에서 모델 루트만 새 프리팹으로 교체하고,
/// 프리팹 전체에 흩어진 Animator / SkinnedMeshRenderer / Transform 등 참조를 새 계층에 맞게 다시 연결합니다.
/// </summary>
public class CharacterGenerator : EditorWindow
{
    GameObject templatePrefab;
    GameObject newModelPrefab;
    string slotObjectName = "TheProtegonist";
    string newPrefabFileName = "Player.NewCharacter";

    [MenuItem("Tools/Character/모델 스왑 프리팹 생성")]
    static void Open() => GetWindow<CharacterGenerator>("캐릭터 프리팹 생성");

    void OnGUI()
    {
        EditorGUILayout.LabelField("템플릿 프리팹을 복사해 모델 슬롯만 새 프리팹으로 바꿉니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(4);

        templatePrefab = (GameObject)EditorGUILayout.ObjectField("템플릿 프리팹", templatePrefab, typeof(GameObject), false);
        newModelPrefab = (GameObject)EditorGUILayout.ObjectField("새 모델 프리팹", newModelPrefab, typeof(GameObject), false);

        EditorGUILayout.HelpBox(
            "슬롯 이름: 템플릿 루트 아래에서 교체할 모델 루트 오브젝트 이름입니다. 비우면 PlayerModel 또는 Animator가 있는 첫 모델 루트를 찾습니다.",
            MessageType.Info);
        slotObjectName = EditorGUILayout.TextField("모델 슬롯 이름 (선택)", slotObjectName);

        newPrefabFileName = EditorGUILayout.TextField("저장 파일 이름", newPrefabFileName);

        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(templatePrefab == null || newModelPrefab == null))
        {
            if (GUILayout.Button("프리팹 생성", GUILayout.Height(32)))
                GeneratePrefab();
        }
    }

    void GeneratePrefab()
    {
        string templatePath = AssetDatabase.GetAssetPath(templatePrefab);
        string modelPath = AssetDatabase.GetAssetPath(newModelPrefab);

        if (string.IsNullOrEmpty(templatePath) || !templatePath.EndsWith(".prefab"))
        {
            EditorUtility.DisplayDialog("오류", "템플릿은 프로젝트 안의 프리팹 에셋이어야 합니다.", "확인");
            return;
        }

        if (string.IsNullOrEmpty(modelPath))
        {
            EditorUtility.DisplayDialog("오류", "새 모델 프리팹 경로를 찾을 수 없습니다.", "확인");
            return;
        }

        string dir = System.IO.Path.GetDirectoryName(templatePath).Replace('\\', '/');
        string savePath = $"{dir}/{newPrefabFileName}.prefab";
        savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

        GameObject root = PrefabUtility.LoadPrefabContents(templatePath);
        try
        {
            Transform rootT = root.transform;
            Transform oldSlot = FindModelSlot(rootT, slotObjectName);
            if (oldSlot == null)
            {
                EditorUtility.DisplayDialog("오류", "모델 슬롯을 찾지 못했습니다. 슬롯 이름을 확인하거나 템플릿 구조에 PlayerModel/Animator를 두세요.", "확인");
                return;
            }

            Transform parent = oldSlot.parent;
            int siblingIndex = oldSlot.GetSiblingIndex();
            Vector3 lp = oldSlot.localPosition;
            Quaternion lr = oldSlot.localRotation;
            Vector3 ls = oldSlot.localScale;

            GameObject newInstance = PrefabUtility.InstantiatePrefab(newModelPrefab, parent) as GameObject;
            if (newInstance == null)
            {
                EditorUtility.DisplayDialog("오류", "새 모델을 인스턴스화하지 못했습니다.", "확인");
                return;
            }

            Transform newSlot = newInstance.transform;
            newSlot.SetSiblingIndex(siblingIndex);
            newSlot.localPosition = lp;
            newSlot.localRotation = lr;
            newSlot.localScale = ls;

            Animator newAnim = newSlot.GetComponentInChildren<Animator>();
            if (newAnim == null)
            {
                Object.DestroyImmediate(newInstance);
                EditorUtility.DisplayDialog("오류", "새 모델에 Animator가 없습니다.", "확인");
                return;
            }

            RemapSubtreeReferences(root, oldSlot, newSlot);

            SyncPlayerModelIfPresent(newSlot, newAnim);

            Object.DestroyImmediate(oldSlot.gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, savePath);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("완료", $"저장됨:\n{savePath}", "확인");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(savePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static Transform FindModelSlot(Transform root, string slotName)
    {
        if (!string.IsNullOrWhiteSpace(slotName))
        {
            var direct = root.Find(slotName);
            if (direct != null)
                return direct;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root && t.name == slotName && t.GetComponent<Animator>() != null)
                    return t;
            }
        }

        var pm = root.GetComponentInChildren<PlayerModel>(true);
        if (pm != null)
            return pm.transform;

        foreach (Transform c in root)
        {
            if (c.GetComponent<Animator>() != null)
                return c;
        }

        return root.GetComponentInChildren<Animator>(true)?.transform;
    }

    static string GetRelativePath(Transform ancestor, Transform descendant)
    {
        if (descendant == null || ancestor == null)
            return null;
        if (!descendant.IsChildOf(ancestor) && descendant != ancestor)
            return null;

        if (descendant == ancestor)
            return "";

        var parts = new List<string>();
        Transform t = descendant;
        while (t != null && t != ancestor)
        {
            parts.Add(t.name);
            t = t.parent;
        }

        if (t != ancestor)
            return null;

        parts.Reverse();
        return string.Join("/", parts);
    }

    static Transform ResolvePath(Transform newRoot, string relativePath)
    {
        if (newRoot == null)
            return null;
        if (string.IsNullOrEmpty(relativePath))
            return newRoot;
        return newRoot.Find(relativePath);
    }

    static Object MapObject(Transform oldRoot, Transform newRoot, Object obj)
    {
        if (obj == null)
            return null;

        switch (obj)
        {
            case Transform tr:
                if (tr == oldRoot)
                    return newRoot;
                if (!tr.IsChildOf(oldRoot))
                    return obj;
                {
                    string rel = GetRelativePath(oldRoot, tr);
                    return ResolvePath(newRoot, rel);
                }
            case GameObject go:
                {
                    Transform gt = go.transform;
                    if (gt == oldRoot)
                        return newRoot.gameObject;
                    if (!gt.IsChildOf(oldRoot))
                        return obj;
                    string rel = GetRelativePath(oldRoot, gt);
                    var nt = ResolvePath(newRoot, rel);
                    return nt != null ? nt.gameObject : null;
                }
            case Component c:
                {
                    Transform ct = c.transform;
                    if (ct == oldRoot)
                    {
                        var onNewRoot = newRoot.GetComponent(c.GetType());
                        return onNewRoot != null ? onNewRoot : obj;
                    }

                    if (!ct.IsChildOf(oldRoot))
                        return obj;

                    string rel = GetRelativePath(oldRoot, ct);
                    var newT = ResolvePath(newRoot, rel);
                    if (newT == null)
                        return null;

                    var nc = newT.GetComponent(c.GetType());
                    return nc != null ? nc : obj;
                }
            default:
                return obj;
        }
    }

    static void RemapSubtreeReferences(GameObject root, Transform oldSlotRoot, Transform newSlotRoot)
    {
        var components = root.GetComponentsInChildren<Component>(true);
        foreach (var comp in components)
        {
            if (comp == null)
                continue;

            SerializedObject so = new SerializedObject(comp);
            SerializedProperty prop = so.GetIterator();
            bool changed = false;

            while (prop.Next(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    Object cur = prop.objectReferenceValue;
                    Object mapped = MapObject(oldSlotRoot, newSlotRoot, cur);
                    if (mapped != cur)
                    {
                        prop.objectReferenceValue = mapped;
                        changed = true;
                    }
                }
            }

            if (changed)
                so.ApplyModifiedProperties();
        }
    }

    static void SyncPlayerModelIfPresent(Transform modelRoot, Animator fallbackAnimator)
    {
        var pm = modelRoot.GetComponent<PlayerModel>();
        if (pm == null)
            pm = modelRoot.gameObject.AddComponent<PlayerModel>();

        var anim = modelRoot.GetComponentInChildren<Animator>();
        if (anim == null)
            anim = fallbackAnimator;

        var smr = modelRoot.GetComponentInChildren<SkinnedMeshRenderer>();

        SerializedObject so = new SerializedObject(pm);
        so.FindProperty("animator").objectReferenceValue = anim;
        so.FindProperty("meshRenderer").objectReferenceValue = smr;
        so.ApplyModifiedProperties();
    }
}
