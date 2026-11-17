using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

/// <summary>
/// Simple connectivity checker that shows a dialog if internet is not reachable.
/// </summary>
namespace MultiTechStudio.Cooking2D
{
    public class CheckConnection : MonoBehaviour
    {
        public GameObject NoInternetDialog;
        public GameObject CheckingConnection;
        public GameObject NoInternetBg;
        string m_ReachabilityText;


        IEnumerator checkInternetConnection(Action<bool> action)
        {
            UnityWebRequest www = UnityWebRequest.Get("https://www.google.com");
            yield return new WaitForEndOfFrame(); 
            yield return www.SendWebRequest();

            if (www.error != null)
            {
                action(false);
            }
            else
            {
                action(true);
            }
        }

        void Start()
        {
            checkForConnectivity();
        }

        private int calls = 0;

        void checkForConnectivity()
        {
            StartCoroutine(checkInternetConnection((isConnected) =>
            {
                if (isConnected)
                {
                    //Pop-Up Close
                    if (NoInternetDialog.activeInHierarchy)
                    {
                        NoInternetDialog.SetActive(false);
                    }
                }
                else
                {
                    if (!NoInternetDialog.activeInHierarchy)
                    {
                        NoInternetDialog.SetActive(true);
                    }

                    calls++;

                    if (calls >= 3)
                    {
                        StartCoroutine(WaitForTime());
                    }
                    else
                    {
                        checkForConnectivity();
                    }
                }
            }));
        }

        IEnumerator WaitForTime()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(5f);

            checkForConnectivity();
        }

        public void QuitGame()
        {
            StartCoroutine(ConnectionStatus());
        }

        IEnumerator ConnectionStatus()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(UnityEngine.Random.Range(4f, 5.1f));

            if (NoInternetDialog.activeInHierarchy)
            {
                NoInternetBg.SetActive(true);
                CheckingConnection.SetActive(false);
            }
        }

        #region Unity Default

        void CheckUpdate()
        {
            //Output the network reachability to the console window
            Debug.Log("Internet : " + m_ReachabilityText);
            //Check if the device cannot reach the internet
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                //Change the Text
                m_ReachabilityText = "Not Reachable.";
            }
            //Check if the device can reach the internet via a carrier data network
            else if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
            {
                m_ReachabilityText = "Reachable via carrier data network.";
            }
            //Check if the device can reach the internet via a LAN
            else if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                m_ReachabilityText = "Reachable via Local Area Network.";
            }
        }

        #endregion
    }
}