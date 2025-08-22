using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrassComputeShader))]
public class GrassComputeShaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GrassComputeShader grassSystem = (GrassComputeShader)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Controls", EditorStyles.boldLabel);
        
        // 잔디 생성 버튼
        if (GUILayout.Button("Generate Grass in Editor"))
        {
            grassSystem.GenerateGrassInEditor();
        }
        
        // 지형 분석 버튼
        if (GUILayout.Button("Analyze Terrain"))
        {
            grassSystem.AnalyzeTerrain();
        }
        
        // Material 설정 체크 버튼
        if (GUILayout.Button("Check Material Settings"))
        {
            grassSystem.CheckMaterialSettings();
        }
        
        // 기본 Material 생성 버튼
        if (GUILayout.Button("Create Default Grass Material"))
        {
            grassSystem.CreateDefaultGrassMaterial();
        }
        
        // Standard 쉐이더로 변경 버튼
        if (GUILayout.Button("Switch to Standard Shader"))
        {
            grassSystem.SwitchToStandardShader();
        }
        
        // 렌더링 상태 체크 버튼
        if (GUILayout.Button("Check Rendering Status"))
        {
            grassSystem.CheckRenderingStatus();
        }
        
        // 잔디 제거 버튼
        if (GUILayout.Button("Clear All Grass"))
        {
            grassSystem.ClearAllGrass();
        }
        
        // 잔디 정보 표시
        if (grassSystem.GetCurrentGrassCount() > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Current Grass Count: {grassSystem.GetCurrentGrassCount()}", EditorStyles.boldLabel);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Use 'Generate Grass in Editor' to create grass without entering Play mode.", MessageType.Info);
    }
}
