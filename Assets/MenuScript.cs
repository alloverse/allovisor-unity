using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuParameters
{
    public static string urlToOpen;
    public static string lastError;
}

public class MenuScript : MonoBehaviour {
    public GameObject panel;
    public GameObject buttonPrefab;
    public GameObject errorPrefab;

	void Start () {
        SetUrlCallback(Marshal.GetFunctionPointerForDelegate(new UrlCallback(this.UrlHandler)));

        if(MenuParameters.lastError != null) {
            GameObject text = (GameObject)Instantiate(errorPrefab);
            text.transform.SetParent(panel.transform);
            text.GetComponent<Text>().text = MenuParameters.lastError;
            MenuParameters.lastError = null;
        }

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

    private void UrlHandler(string url) {
        ConnectToUrl(url);
    }
    private void ConnectToUrl(string url) {
        MenuParameters.urlToOpen = url;
        print("Opening url " + url);
        SceneManager.LoadScene("Scenes/NetworkScene");
    }
    public void ConnectTo() {
        string url = EventSystem.current.currentSelectedGameObject.name;
        ConnectToUrl(url);
    }

    [DllImport("AllovisorNativeExtensions")]
    public unsafe static extern void SetUrlCallback(IntPtr UrlCallback);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void UrlCallback(string url);
}
