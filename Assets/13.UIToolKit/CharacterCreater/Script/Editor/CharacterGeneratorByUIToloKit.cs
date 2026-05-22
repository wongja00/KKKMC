using Bezi.Sidekick;
using Mirror.Examples.CharacterSelection;
using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public enum CharacterKind
{
    Player,
    Enemy
}

public class CharacterGeneratorByUIToloKit : EditorWindow
{
    private TextField characterNameField;
    private ObjectField characterModel;
    private ObjectField characterAvatar;
    private EnumField characterType;

    //스탯 필드
    private IntegerField identity;
    private IntegerField strength;
    private IntegerField defense;
    private IntegerField agility;
    private IntegerField intellegency;
    private FloatField attackSpeed;
    private FloatField critChance;
    private FloatField critDamage;
    private ObjectField thumnailField;
    private TextField charDesc;


    private VisualElement previewArea;
    private Button createButton;

    private PreviewRenderUtility previewUtil;
    private GameObject previewInstance;
    private GameObject basePrefab;
    private string playerAnimatorPath = "Assets/3.Character/The Protegonist/Player.controller";
    private string playerBasePath = "Assets/8.prefaps/Player/PlayerBase.prefab";
    private string enemyBasePath = "Assets/8.prefaps/Character/Enemy/EnemyBase.prefab";
    private string playerPrefabPath = "Assets/8.prefaps/Player/";
    private string enemyPrefabPath = "Assets/8.prefaps/Character/Enemy/";


    private Vector2 dragDelta;
    private Vector2 posDelta = new Vector2(0, 1);
    private float cameraDepth = -10;
    CharacterKind selectedKind = CharacterKind.Player;


    [MenuItem("Tools/Character Creator")]
    public static void ShowWindow()
    {
        CharacterGeneratorByUIToloKit wnd = GetWindow<CharacterGeneratorByUIToloKit>();
        wnd.titleContent = new GUIContent("캐릭터 생성 툴");

        wnd.minSize = new Vector2(800, 600);
    }

    private void OnEnable()
    {
        basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/8.prefaps/Player/PlayerBase.prefab");

        previewUtil = new PreviewRenderUtility(true);

        previewUtil.camera.transform.position = new Vector3(0, 1, cameraDepth);
        previewUtil.camera.transform.LookAt(new Vector3(0, 1, 0));
        previewUtil.camera.nearClipPlane = 0.1f;
        previewUtil.camera.farClipPlane = 100f;
        previewUtil.camera.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        previewUtil.camera.clearFlags = CameraClearFlags.Color;

        var cameraData = previewUtil.camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (cameraData == null)
        {
            cameraData = previewUtil.camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        }

        cameraData.renderShadows = false; // 성능을 위해 그림자 끔
 
        previewUtil.lights[0].intensity = 1.2f;
        previewUtil.lights[0].transform.rotation = Quaternion.Euler(30, 30, 0);
    }

    private void OnDisable()
    {
        if(previewUtil != null)
        {
            previewUtil.Cleanup();
            previewUtil = null;
        }
        DestroyPreviewInstance();
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/13.UIToolKit/CharacterCreater/CharacterCreater.uxml");
 
        if(visualTree != null)
        {
            VisualElement labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);

            characterNameField = root.Q<TextField>("NameField");
            characterModel = root.Q<ObjectField>("CharacterField");
            characterAvatar = root.Q<ObjectField>("AvatarField");
            characterType = root.Q<EnumField>("TypeField");
            previewArea = root.Q<VisualElement>("PreviewArea");

            //스크립터블 오브젝트용 캐릭터 스탯 필드
            identity = root.Q<IntegerField>("IDField");
            strength = root.Q<IntegerField>("StrengthField");
            defense = root.Q<IntegerField>("DefenseField");
            agility = root.Q<IntegerField>("AgilityField");
            intellegency = root.Q<IntegerField>("IntField");
            attackSpeed = root.Q<FloatField>("ASField");
            critChance = root.Q<FloatField>("CCField");
            critDamage = root.Q<FloatField>("CDField");
            thumnailField = root.Q<ObjectField>("ThumnailField");
            charDesc = root.Q<TextField>("DescriptionField");

            createButton = root.Q<Button>("CreateButton");




            //IMGUIContainer
            IMGUIContainer previewContainer = new IMGUIContainer(OnPreviewGUI);
            previewContainer.style.flexGrow = 1;
            previewArea.Add(previewContainer);

            characterModel.RegisterValueChangedCallback(evt => 
            {
                UpdatePreviewInstance(evt.newValue as GameObject);
            });

            characterType.RegisterCallback<ChangeEvent<CharacterKind>>(evt =>
            {
                selectedKind = (CharacterKind)evt.newValue;
                string path = selectedKind == CharacterKind.Player ? playerBasePath : enemyBasePath;

                basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            });

            createButton.RegisterCallback<ClickEvent>(evt =>
            {
                if(characterModel.value != null)
                {
                    GameObject prefab = CreateCharacter();
                    CreateCharacterSO(prefab);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "캐릭터 모델을 선택해주세요.", "OK");
                }
            });
        }
        else
        {
            root.Add(new Label("UXML파일을 찾을수 없음"));
        }


    }

    private void OnPreviewGUI()
    {
        if(previewUtil == null) return;

        Rect previewRect = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        DragHandle(previewRect);

        if(Event.current.type == EventType.Repaint)
        {
            previewUtil.BeginPreview(previewRect, GUIStyle.none);

            if(previewInstance != null)
            {
                previewInstance.transform.rotation = Quaternion.Euler(0, dragDelta.x, 0);
                previewUtil.camera.transform.position = new Vector3(0, posDelta.y, cameraDepth);

                previewUtil.camera.Render();
                //previewUtil.BeginPreview(GUILayoutUtility.GetLastRect(), GUIStyle.none);
            }
        
            Texture resultTexture = previewUtil.EndPreview();
            GUI.DrawTexture(previewRect, resultTexture);
        }
    }

    void UpdatePreviewInstance(GameObject asset)
    {
        DestroyPreviewInstance();

        if(asset != null)
        {
            previewInstance = previewUtil.InstantiatePrefabInScene(asset);
            previewInstance.transform.position = Vector3.zero;
            
        }
    }

    void DestroyPreviewInstance()
    {
        if(previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    public GameObject CreateCharacter()
    {
        if(selectedKind == CharacterKind.Player)
        {
            string path = playerPrefabPath + characterNameField.value + ".prefab";
            GameObject newCharacter = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            newCharacter.name = characterNameField.value;
            newCharacter.transform.position = Vector3.zero;
    
            GameObject modelInstance = PrefabUtility.InstantiatePrefab(characterModel.value) as GameObject;
            PrefabUtility.UnpackPrefabInstance(modelInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            modelInstance.transform.SetParent(newCharacter.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localPosition = new Vector3(0, -1, 0);

            modelInstance.AddComponent<PlayerModel>();
            if(modelInstance.GetComponent<Animator>() == null)
            {
                Animator anim = modelInstance.AddComponent<Animator>();
                anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(playerAnimatorPath);
                anim.avatar = characterAvatar.value as Avatar;
            }

            Transform rigTransform = modelInstance.transform.Find("rig");
            SetEffectSocket(rigTransform);

            GameObject savedAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(newCharacter, path, InteractionMode.AutomatedAction);
            if (savedAsset != null)
            {
                PrefabUtility.ApplyPrefabInstance(newCharacter, InteractionMode.AutomatedAction);
            }
            DestroyImmediate(newCharacter);
            return savedAsset;

        }
        else if(selectedKind == CharacterKind.Enemy)
        {
                string path = enemyPrefabPath + characterNameField.value + ".prefab";
                GameObject newCharacter = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
                newCharacter.name = characterNameField.value;
                newCharacter.transform.position = Vector3.zero;
    
                GameObject modelInstance = PrefabUtility.InstantiatePrefab(characterModel.value) as GameObject;
                PrefabUtility.UnpackPrefabInstance(modelInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                modelInstance.transform.SetParent(newCharacter.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;

                GameObject savedAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(newCharacter, path, InteractionMode.AutomatedAction);
            if (savedAsset != null)
            {
                PrefabUtility.ApplyPrefabInstance(newCharacter, InteractionMode.AutomatedAction);
            }
            DestroyImmediate(newCharacter);
            return savedAsset;

        }

        return null;


    }

    void SetEffectSocket(Transform transform)
    {
        CharacterEffectHandler cEH = transform.parent.parent.GetComponentInChildren<CharacterEffectHandler>();

        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            //오른손
            string n = child.name;
            bool hasHand = n.IndexOf("hand", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasFoot = n.IndexOf("foot", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasRight = n.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.IndexOf("_r", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.EndsWith("r", StringComparison.OrdinalIgnoreCase);

            bool hasleft = n.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.IndexOf("_l", StringComparison.OrdinalIgnoreCase) >= 0
                         || n.EndsWith("l", StringComparison.OrdinalIgnoreCase);

            if (hasHand && hasRight)
            {
                EffectSocket socket = child.AddComponent<EffectSocket>();
                socket.part = EffectSocketPart.RightHand;
                socket.socketTransfrom = child;
                cEH.effectSocketList.Add(socket);
            }
            //왼손
            else if (hasHand && hasleft)
            {
                EffectSocket socket = child.AddComponent<EffectSocket>();
                socket.part = EffectSocketPart.LeftHand;
                socket.socketTransfrom = child;
                cEH.effectSocketList.Add(socket);
            }
            //오른발
            else if (hasFoot && hasRight)
            {
                EffectSocket socket = child.AddComponent<EffectSocket>();
                socket.part = EffectSocketPart.RightFoot;
                socket.socketTransfrom = child;
                cEH.effectSocketList.Add(socket);
            }
            //왼발
            else if (hasFoot && hasleft)
            {
                EffectSocket socket = child.AddComponent<EffectSocket>();
                socket.part = EffectSocketPart.LeftFoot;
                socket.socketTransfrom = child;
                cEH.effectSocketList.Add(socket);
            }
        }

    }

    void CreateCharacterSO(GameObject prefab)
    {
        CharacterInfo newCharacterData = ScriptableObject.CreateInstance<CharacterInfo>();
        string path = "Assets/6.ScritableObject/CharacterData/" + identity.value.ToString() + "."  + characterNameField.value + ".asset";

        newCharacterData.name = characterNameField.value;
        newCharacterData.ID = identity.value;
        newCharacterData.status = new Status
        {
            strength = strength.value,
            defense = defense.value,
            agility = agility.value,
            intelligence = intellegency.value,
            attackSpeed = attackSpeed.value,
            critChance = critChance.value,
            critDamage = critDamage.value
        };
        newCharacterData.thumnail = thumnailField.value as Sprite;
        newCharacterData.prefab = prefab;
        newCharacterData.characterName = characterNameField.value;
        newCharacterData.desc = charDesc.value;

        AssetDatabase.CreateAsset(newCharacterData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("생성완료", $"{newCharacterData.name} 캐릭터가 생성되었습니다.", "예아");
    }

    void DragHandle(Rect rect)
    {
        Event evt = Event.current;

        if(!rect.Contains(evt.mousePosition))
            return;

        if(evt.type == EventType.MouseDrag && evt.button == 0)
        {
            dragDelta.x -= evt.delta.x * 0.7f;

            Repaint();
        }

        if(evt.type == EventType.MouseDrag && evt.button == 1)
        {
            posDelta.y += evt.delta.y * 0.01f;

            Repaint();
        }

        if(evt.type == EventType.ScrollWheel)
        {
            cameraDepth -= evt.delta.y * 0.1f;

            evt.Use();
            Repaint();
        }
    }
}
