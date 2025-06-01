using UnityEngine;

public class LogoutActions : MonoBehaviour
{
    public void LogoutToLogin() => FlowManager.Instance?.LogoutToLogin();
    public void LogoutToCharacterSelect() => FlowManager.Instance?.LogoutToCharacterSelect();
    public void LogoutToServerSelect() => FlowManager.Instance?.LogoutToServerSelect();
}
