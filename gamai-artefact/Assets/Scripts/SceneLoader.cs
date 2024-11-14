using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private GameObject _loadingScreen;
    [SerializeField] private SceneNames _sceneName;

    // Start is called before the first frame update
    void Awake()
    {
        _loadingScreen = GameObject.Find("LoadingScreen");
    }

    public async void ChangeScene()
    {
        var scene = _sceneName == SceneNames.NextLevel 
            ? SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1) 
            : SceneManager.LoadSceneAsync(_sceneName.ToString());
        scene.allowSceneActivation = false; 
        _loadingScreen.transform.GetChild(0).gameObject.SetActive(true); 
        var loadingScreenBackground = _loadingScreen.GetComponentInChildren<Image>(); 
        var alpha = 0f; 
        while (alpha < 1f)
        {
            alpha += 2f * Time.deltaTime;
            loadingScreenBackground.color = new Color(0f, 0f, 0f, alpha);
            await Task.Yield();
        }

        do
        {
            await Task.Yield();
        } while (scene.progress < 0.9f);

        scene.allowSceneActivation = true;

        while (alpha > 0f)
        {
            alpha -= 2f * Time.deltaTime;
            loadingScreenBackground.color = new Color(0f, 0f, 0f, alpha);
            await Task.Yield();
        }

        _loadingScreen.transform.GetChild(0).gameObject.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

    public enum SceneNames {
        MainMenu,
        LevelSelect,
        HowToPlay,
        Level1,
        Level2,
        Level3,
        Level4,
        Level5,
        Level6,
        Level7,
        Level8,
        Level9,
        Level10,
        NextLevel
    }
