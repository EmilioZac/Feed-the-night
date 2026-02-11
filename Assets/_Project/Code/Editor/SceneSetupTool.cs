using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using FeedTheNight.Controllers;

public class SceneSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Gym")]
    public static void SetupGym()
    {
        // 1. Create Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10, 1, 10);
        ground.transform.position = Vector3.zero;

        // 2. Create Player
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player_Robot";
        player.transform.position = new Vector3(0, 1.1f, 0); // Slightly above ground

        // Cleanup default collider to avoid conflict/redundancy with CharacterController
        if (player.GetComponent<CapsuleCollider>())
            DestroyImmediate(player.GetComponent<CapsuleCollider>());

        // 3. Add Components
        CharacterController cc = player.AddComponent<CharacterController>();
        
        // Input System
        PlayerInput pi = player.AddComponent<PlayerInput>();
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (actions != null)
        {
            pi.actions = actions;
            pi.defaultActionMap = "Player";
        }
        else
        {
            Debug.LogError("Could not find InputSystem_Actions.inputactions at 'Assets/InputSystem_Actions.inputactions'. Please assign manually.");
        }

        // Player Controller
        player.AddComponent<PlayerController>();

        // 4. Create Camera (Basic Follow)
        // Check if camera exists
        if (Camera.main == null)
        {
            GameObject camera = new GameObject("Main Camera");
            camera.tag = "MainCamera";
            camera.AddComponent<Camera>();
            camera.AddComponent<AudioListener>();
            camera.transform.position = new Vector3(0, 3, -5);
            camera.transform.LookAt(player.transform);
            camera.transform.SetParent(player.transform); // Simple childing for prototype
        }
        
        Selection.activeGameObject = player;
         if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
        
        Debug.Log("Gym Setup Complete! Ground and Player_Robot created.");
    }
}
