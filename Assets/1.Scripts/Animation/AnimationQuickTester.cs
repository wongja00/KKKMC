using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class AnimationQuickTester : MonoBehaviour
{
    public AnimationClip testClip; // 테스트하고 싶은 클립을 드래그 앤 드롭
    private PlayableGraph graph;
    private AnimationPlayableOutput output;

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(AnimationQuickTester))]
    public class AnimationQuickTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AnimationQuickTester tester = (AnimationQuickTester)target;
            if (GUILayout.Button("테스트 클립 재생"))
            {
                tester.Play();
            }
        }
    }
#endif

    public void Play()
    {
        if (graph.IsValid()) graph.Destroy();

        graph = PlayableGraph.Create("TestGraph");
        output = AnimationPlayableOutput.Create(graph, "Output", GetComponent<Animator>());
        
        var clipPlayable = AnimationClipPlayable.Create(graph, testClip);
        output.SetSourcePlayable(clipPlayable);
        
        graph.Play();
    }
    

    private void OnDisable() { if (graph.IsValid()) graph.Destroy(); }
}