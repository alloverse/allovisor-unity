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

public struct PlaceDescriptor
{
    public string url;
    public string name;
    public PlaceDescriptor(string url, string name)
    {
        this.url = url;
        this.name = name;
    }
}

public class VisorSettings
{
    public PlaceDescriptor[] previousPlaces;
    public PlaceDescriptor[] PreviousPlaces {
        get {
            return previousPlaces;
        }
        private set {
            previousPlaces = value;
            Save();
        }
    }
    public void addPlace(PlaceDescriptor place) {
        List<PlaceDescriptor> places = new List<PlaceDescriptor>(this.PreviousPlaces);
        if(places.Count > 10) {
            places.RemoveAt(9);
        }
        places.Insert(0, place);
        this.PreviousPlaces = places.ToArray();
    }

    public static VisorSettings GlobalSettings;
    public static VisorSettings Load() {
        string prefs = PlayerPrefs.GetString("VisorSettings");
        if (prefs != null)
        {
            return LitJson.JsonMapper.ToObject<VisorSettings>(prefs);
        } else {
            return new VisorSettings();
        }
    }
    public void Save() {
        PlayerPrefs.SetString("VisorSettings", LitJson.JsonMapper.ToJson(this));
    }
}

public class MenuScript : MonoBehaviour {
    public GameObject panel;
    public GameObject buttonPrefab;
    public GameObject errorPrefab;

	void Start () {
        SetUrlCallback(Marshal.GetFunctionPointerForDelegate(new UrlCallback(this.UrlHandler)));
        VisorSettings.GlobalSettings = VisorSettings.Load();

        if (MenuParameters.lastError != null) {
            GameObject text = (GameObject)Instantiate(errorPrefab);
            text.transform.SetParent(panel.transform);
            text.GetComponent<Text>().text = MenuParameters.lastError;
            MenuParameters.lastError = null;
        }

        foreach(PlaceDescriptor place in VisorSettings.GlobalSettings.PreviousPlaces)
        {
            GameObject button = (GameObject)Instantiate(buttonPrefab);
            button.transform.SetParent(panel.transform);
            button.GetComponent<Button>().onClick.AddListener(ConnectTo);
            button.name = place.url;
            button.transform.GetChild(0).GetComponent<Text>().text = place.name ?? place.url;
        }
    }

    // Update is called once per frame
    void Update () {
		
	}

    private void UrlHandler(string url) {
        ConnectToUrl(url);
    }
    public void ConnectTo() {
        string url = EventSystem.current.currentSelectedGameObject.name;
        ConnectToUrl(url);
    }
    private void ConnectToUrl(string url)
    {
        MenuParameters.urlToOpen = url;
        print("Opening url " + url);
        SceneManager.LoadScene("Scenes/NetworkScene");
        VisorSettings.GlobalSettings.addPlace(new PlaceDescriptor(url, null));
    }

    [DllImport("AllovisorNativeExtensions")]
    public unsafe static extern void SetUrlCallback(IntPtr UrlCallback);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void UrlCallback(string url);
}
