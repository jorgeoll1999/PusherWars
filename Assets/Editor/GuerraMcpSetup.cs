using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;
[InitializeOnLoad]
public static class GuerraMcpSetup
{
    static GuerraMcpSetup() { EditorApplication.delayCall += Connect; }
    static async void Connect()
    {
        if (Application.isBatchMode) return;
        if (SessionState.GetBool("GuerraMcpSetup.ReconnectV2", false)) return;
        SessionState.SetBool("GuerraMcpSetup.ReconnectV2", true);
        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
        try { await MCPServiceLocator.Bridge.StopAsync(); Debug.Log("Guerra MCP connection: " + await MCPServiceLocator.Bridge.StartAsync()); }
        catch (System.Exception e) { Debug.LogException(e); }
    }
}
