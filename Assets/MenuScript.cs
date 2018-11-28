using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
            button.transform.GetChild(0).GetComponent<Text>().text = "alloplace://localhost:21337";
            button.name = "alloplace://localhost:21337";
        }
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void ConnectTo() {
        print(EventSystem.current.currentSelectedGameObject.name);
    }
}
