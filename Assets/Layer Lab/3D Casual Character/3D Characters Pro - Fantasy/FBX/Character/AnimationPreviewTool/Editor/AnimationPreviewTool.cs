using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class UltimateAnimationPreviewer : EditorWindow
{
    private GameObject character;
    private AnimationClip clip;
    private Editor characterPreviewEditor;

    // Playback state
    private bool isPlaying;
    private float animationTime;
    private double lastFrameTime;

    // Enhanced settings
    private bool autoPlayEnabled = true;
    private bool forceLoop;
    private float playbackSpeed = 1.0f;

    [MenuItem("Tools/Bill Utils/Ultimate Animation Previewer")]
    public static void ShowWindow()
    {
        GetWindow<UltimateAnimationPreviewer>("Ultimate Previewer");
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        StopAndCleanUp();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawObjectFields();

        if (IsReadyToPreview())
        {
            DrawPreviewWindow();
            DrawSettings();
            DrawPlaybackControls();
            DrawAnimatorActions(); // NEW: Draw action buttons
        }
        else
        {
            DrawHelpBox();
        }
    }

    private void OnEditorUpdate()
    {
        if (isPlaying && IsReadyToPreview())
        {
            UpdateAnimationTime();
            SampleAnimationClip();
            Repaint();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Ultimate Animation Previewer", EditorStyles.boldLabel);
        EditorGUILayout.Space();
    }

    private void DrawObjectFields()
    {
        EditorGUI.BeginChangeCheck();
        GameObject newCharacter = (GameObject)EditorGUILayout.ObjectField("Character (with Animator)", character, typeof(GameObject), true);
        AnimationClip newClip = (AnimationClip)EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), false);

        if (EditorGUI.EndChangeCheck())
        {
            StopAndCleanUp();
            character = newCharacter;
            clip = newClip;
            InitializePreview();
        }
    }

    private void DrawPreviewWindow()
    {
        GUIStyle previewBackground = new GUIStyle("box");
        Rect previewRect = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (characterPreviewEditor != null)
        {
            characterPreviewEditor.OnInteractivePreviewGUI(previewRect, previewBackground);
        }
    }

    private void DrawSettings()
    {
        EditorGUILayout.BeginHorizontal();
        autoPlayEnabled = EditorGUILayout.ToggleLeft("Auto Play", autoPlayEnabled, GUILayout.Width(100));
        forceLoop = EditorGUILayout.ToggleLeft("Force Loop", forceLoop, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        playbackSpeed = EditorGUILayout.Slider("Playback Speed", playbackSpeed, 0.1f, 3.0f);
        EditorGUILayout.Space();
    }

    private void DrawPlaybackControls()
    {
        EditorGUI.BeginChangeCheck();
        float newTime = EditorGUILayout.Slider(animationTime, 0f, clip.length);
        if (EditorGUI.EndChangeCheck())
        {
            animationTime = newTime;
            isPlaying = false;
            SampleAnimationClip();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(isPlaying ? "Pause" : "Play", GUILayout.Width(80)))
        {
            TogglePlayPause();
        }
        if (GUILayout.Button("Stop", GUILayout.Width(80)))
        {
            StopPlayback();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAnimatorActions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animator Actions", EditorStyles.boldLabel);

        AnimatorController controller = GetAnimatorController();
        EditorGUI.BeginDisabledGroup(controller == null);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add as New State"))
        {
            AddClipToAnimatorAsState(controller);
        }
        if (GUILayout.Button("Add as New Blend Tree"))
        {
            AddClipToAnimatorAsBlendTree(controller);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        if (controller == null)
        {
            EditorGUILayout.HelpBox("Character must have a valid Animator Controller assigned.", MessageType.Warning);
        }
    }

    private void InitializePreview()
    {
        if (character == null || clip == null) return;
        if (character.GetComponent<Animator>() == null)
        {
            Debug.LogError("The assigned GameObject must have an Animator component.");
            character = null;
            return;
        }

        characterPreviewEditor = Editor.CreateEditor(character);
        AnimationMode.StartAnimationMode();
        animationTime = 0;

        if (autoPlayEnabled)
        {
            TogglePlayPause();
        }
        else
        {
            isPlaying = false;
            SampleAnimationClip();
        }
    }

    private void StopAndCleanUp()
    {
        if (AnimationMode.InAnimationMode())
        {
            AnimationMode.StopAnimationMode();
        }
        if (characterPreviewEditor != null)
        {
            DestroyImmediate(characterPreviewEditor);
            characterPreviewEditor = null;
        }
        isPlaying = false;
    }

    private void SampleAnimationClip()
    {
        if (!IsReadyToPreview()) return;
        AnimationMode.SampleAnimationClip(character, clip, animationTime);
    }

    private void UpdateAnimationTime()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentTime - lastFrameTime) * playbackSpeed;
        lastFrameTime = currentTime;

        animationTime += deltaTime;
        bool shouldLoop = forceLoop || clip.isLooping;
        if (animationTime > clip.length)
        {
            animationTime = shouldLoop ? animationTime % clip.length : clip.length;
            if (!shouldLoop) { isPlaying = false; }
        }
    }

    private void TogglePlayPause()
    {
        isPlaying = !isPlaying;
        if (isPlaying)
        {
            lastFrameTime = EditorApplication.timeSinceStartup;
            if (!forceLoop && !clip.isLooping && Mathf.Approximately(animationTime, clip.length))
            {
                animationTime = 0;
            }
        }
    }

    private void StopPlayback()
    {
        isPlaying = false;
        animationTime = 0;
        SampleAnimationClip();
    }

    private bool IsReadyToPreview()
    {
        return character != null && clip != null && AnimationMode.InAnimationMode();
    }

    private AnimatorController GetAnimatorController()
    {
        if (character == null) return null;
        var animator = character.GetComponent<Animator>();
        if (animator == null) return null;
        return animator.runtimeAnimatorController as AnimatorController;
    }

    private void AddClipToAnimatorAsState(AnimatorController controller)
    {
        Undo.RecordObject(controller, "Add Animation State");
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Vector3 statePosition = new Vector3(300, 120 * stateMachine.states.Length, 0);

        AnimatorState newState = stateMachine.AddState(clip.name, statePosition);
        newState.motion = clip;

        EditorUtility.SetDirty(controller);
        Debug.Log($"State '{clip.name}' was added to controller '{controller.name}'.", controller);
    }

    private void AddClipToAnimatorAsBlendTree(AnimatorController controller)
    {
        Undo.RecordObject(controller, "Add Blend Tree State");

        // Create the Blend Tree asset itself
        BlendTree blendTree = new BlendTree
        {
            name = clip.name + " BlendTree",
            hideFlags = HideFlags.HideInHierarchy // Prevents cluttering the Project window
        };

        // Add the clip to the blend tree
        blendTree.AddChild(clip);

        // Add the blend tree as a sub-asset to the controller file
        AssetDatabase.AddObjectToAsset(blendTree, controller);

        // Create the state in the state machine
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Vector3 statePosition = new Vector3(550, 120 * (stateMachine.states.Length), 0);
        AnimatorState newState = stateMachine.AddState(blendTree.name, statePosition);
        newState.motion = blendTree;

        EditorUtility.SetDirty(controller);
        Debug.Log($"Blend Tree State '{blendTree.name}' was added to controller '{controller.name}'.", controller);
    }

    private void DrawHelpBox()
    {
        EditorGUILayout.HelpBox("Assign a Character (with Animator) and an Animation Clip to begin.", MessageType.Info);
    }
}