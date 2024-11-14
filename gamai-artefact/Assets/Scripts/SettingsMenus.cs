using System.Threading.Tasks;
using UnityEngine;

public class SettingsMenus : MonoBehaviour
{
    [SerializeField] private CanvasRenderer _settingsMenu;
    [SerializeField] private Movement _player;

    public async void ToggleSettingsMenu()
    {
        _settingsMenu.gameObject.SetActive(!_settingsMenu.gameObject.activeSelf);
        await Task.Delay(100); // prevent the click on resume button being taken as an input
        _player._settingsMenuOpen = _settingsMenu.gameObject.activeSelf;
    }
}
