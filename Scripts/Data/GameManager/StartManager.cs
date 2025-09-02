using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartManager : MonoBehaviour
{
	public GameObject HowToPanel;
	public GameObject OptionsMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		HowToPanel.SetActive(false);
		OptionsMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void BackButton()
	{
		HowToPanel.SetActive(false);
		OptionsMenu.SetActive(false);
	}

	public void ClickHowTo()
	{
		HowToPanel.SetActive(true);
		Debug.Log("How To Button Pressed");
	}

	public void ClickOptions()
	{
		OptionsMenu.SetActive(true);
		Debug.Log("Options Button Pressed");
	}

    public void StartGame() 
	{
        SceneManager.LoadScene("Casino");
        Debug.Log("Start Button Pressed");
    }

	public void QuitGame()
	{
		Application.Quit();
		Debug.Log("Quitting Game");
	}
}
