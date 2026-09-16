using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProjectBootstrap
{
    // Auto-installs com.unity.pipeline so an AI coding agent (via the `unity` CLI) can
    // connect to this Editor and drive it live (create/edit GameObjects, run C#, etc.).
    // Safe to leave in the project; it only runs once per Editor session and is a no-op
    // once the package is already resolved. Delete this file any time you no longer want it.
    [InitializeOnLoad]
    public static class ClaudePipelineBootstrap
    {
        const string SessionKey = "ProjectBootstrap.ClaudePipelineBootstrap.Attempted";
        static AddRequest _request;

        static ClaudePipelineBootstrap()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            Debug.Log("[ClaudePipelineBootstrap] Ensuring com.unity.pipeline is installed for AI-agent editor control...");
            _request = Client.Add("com.unity.pipeline");
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (_request == null || !_request.IsCompleted) return;
            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success)
                Debug.Log($"[ClaudePipelineBootstrap] OK: {_request.Result.name}@{_request.Result.version}");
            else
                Debug.LogError($"[ClaudePipelineBootstrap] Failed to add com.unity.pipeline: {_request.Error?.message}");
        }
    }
}
