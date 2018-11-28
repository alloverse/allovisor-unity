using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuParameters
{
    public static string urlToOpen;
}

public class MenuScript : MonoBehaviour {
    public GameObject panel;
    public GameObject buttonPrefab;

	void Start () {
        // todo: load previously opened alloplace URLs from history
        for (int i = 0; i < 1; i++)
        {
            GameObject button = (GameObject)Instantiate(buttonPrefab);
            button.transform.SetParent(panel.transform);
            button.GetComponent<Button>().onClick.AddListener(ConnectTo);
            button.name = "alloplace://localhost:21337";
            button.transform.GetChild(0).GetComponent<Text>().text = button.name;
        }
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void ConnectTo() {
        string url = EventSystem.current.currentSelectedGameObject.name;
        MenuParameters.urlToOpen = url;
        print("Opening url " + url);
        SceneManager.LoadScene("Scenes/NetworkScene");
    }
}
