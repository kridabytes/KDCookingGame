using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Common helpers for UI, animations, scores, and timing.
    /// </summary>
    public static class Extensions
    {
        #region List Extensions

        public static List<T> Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }

            return list;
        }

        public static void ListAdds<T>(ref List<T> list, T element)
        {
            if (list.Count > 0)
            {
                if (!list.Contains(element))
                {
                    list.Add(element);
                }
            }
            else
            {
                list.Add(element);
            }
        }

        public static void ListRemoves<T>(ref List<T> availableObject, T removeObject)
        {
            #region To Remove Particular Object From List

            if (availableObject.Contains(removeObject))
            {
                availableObject.RemoveAt(availableObject.IndexOf(removeObject));
            }

            #endregion

            #region To Check Whether There is Object Or Null Location

            CheckNull(ref availableObject);

            #endregion
        }

        private static void CheckNull<T>(ref List<T> list)
        {
            // Cycle through everything, comparing the types.
            for (var index = list.Count - 1; index >= 0; index--)
            {
                // Get the object.
                object item = list[index];

                // If the item is null, continue.
                if (item == null)
                {
                    list.RemoveAt(index);
                    //continue;
                }

                //// Remove the item if the types match.
                //if (typeof(T) == item.GetType()) list.RemoveAt(index);
            }
        }

        #endregion

        #region Little Extension Methods

        public static bool IsPointerOverGameObject()
        {
            #region For Mouse

            if (EventSystem.current.IsPointerOverGameObject())
                return true;

            #endregion

            #region For ControlFreak2.InputRig.Touch

            //check touch
            if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId) ||
                    EventSystem.current.currentSelectedGameObject != null)
                {
                    return true;
                }
            }

            #endregion

            return false;
        }
        public static int RandomNum(int min, int max)
        {
            int randNum = Random.Range(min, max);
            if (randNum == PlayerPrefs.GetInt("Level"))
            {
                return RandomNum(min, max);
            }
            return randNum;
        }
        public static ParticleSystem ReturnInActiveParticle(ParticleSystem[] particles)
        {
            foreach (var t in particles)
            {
                if (!t.isPlaying)
                {
                    return t;
                }
            }

            return particles[0];
        }


        #region Score Related Methods

        /// <summary>
        /// LittleExtensions.ScoreAdd(ref ScoreVariable);
        /// </summary>
        /// <param name="currentScoreVariable"></param>
        public static int ScoreAdd(float multiplier)
        {
            float adding = Random.Range(2, 4) * multiplier + Random.Range(100, 150);
            int addingAA = Mathf.RoundToInt(adding);
            int randomScore = Random.Range(30, 50) + addingAA;
            ScoreAdder(randomScore);
            return randomScore;
        }

        public static void DiamondAdder(int valueToAdd)
        {
            var target = UiManager.TotalDiamond + valueToAdd;
            UiManager.TotalDiamond = target;
            UiManager.Instance.diamondAddAnim.PlayAnim();

            UiManager.Instance.allCanvas.persistent.diamondTxt.text = UiManager.TotalDiamond.ToString();
            GoogleLogin.instance.SavePlayerDataToFirebase();
        }
        public static void DiamondReducer(int valueToRed)
        {
            var target = UiManager.TotalDiamond - valueToRed;
            UiManager.TotalDiamond = target;

            UiManager.Instance.allCanvas.persistent.diamondTxt.text = UiManager.TotalDiamond.ToString();
        }
        public static void KeyAdder(int valueToAdd)
        {
            var target = UiManager.TotalKey + valueToAdd;
            UiManager.TotalKey = target;
            // UiManager.Instance.keyAddAnim.Stop();
            // UiManager.Instance.keyAddAnim.Play();

            UiManager.Instance.allCanvas.persistent.keyTxt.text = UiManager.TotalKey.ToString();
        }
        public static void KeyReducer(int valueToRed)
        {
            var target = UiManager.TotalKey - valueToRed;
            UiManager.TotalKey = target;

            UiManager.Instance.allCanvas.persistent.keyTxt.text = UiManager.TotalKey.ToString();
        }
        public static void EnergyAdder(int valueToAdd)
        {
            var target = UiManager.TotalEnergy + valueToAdd;
            UiManager.TotalEnergy = target;
            // UiManager.Instance.keyAddAnim.Stop();
            // UiManager.Instance.keyAddAnim.Play();

            UiManager.Instance.allCanvas.persistent.energyTxt.text = UiManager.TotalEnergy.ToString();
            if (GameManager.instance.energyManager.IsUnlimited())
            {
                UiManager.Instance.allCanvas.persistent.energyTxt.text = "Unlimited";
            }

            // Update energy fill bar if it exists
            if (UiManager.Instance.allCanvas.persistent.energyFillBar != null &&
                GameManager.instance.energyManager != null)
            {
                UiManager.Instance.allCanvas.persistent.energyFillBar.fillAmount =
                    (float)UiManager.TotalEnergy / GameManager.instance.energyManager.maxEnergy;
            }
            GoogleLogin.instance.SavePlayerDataToFirebase();

        }
        public static void EnergyReducer(int valueToRed)
        {
            var target = UiManager.TotalEnergy - valueToRed;
            UiManager.TotalEnergy = target;

            UiManager.Instance.allCanvas.persistent.energyTxt.text = UiManager.TotalEnergy.ToString();
            if (GameManager.instance.energyManager.IsUnlimited())
            {
                UiManager.Instance.allCanvas.persistent.energyTxt.text = "Unlimited";
            }

            // Update energy fill bar if it exists
            if (UiManager.Instance.allCanvas.persistent.energyFillBar != null &&
                GameManager.instance.energyManager != null)
            {
                UiManager.Instance.allCanvas.persistent.energyFillBar.fillAmount =
                    (float)UiManager.TotalEnergy / GameManager.instance.energyManager.maxEnergy;
            }
        }

        public static void ScoreAdder(int valueToAdd)
        {
            var target = UiManager.TotalCoins + valueToAdd;
           
            UiManager.TotalCoins = target;
            UiManager.Instance.coinAddAnim.PlayAnim();
            UiManager.Instance.allCanvas.persistent.coinTxt.text = UiManager.TotalCoins.ToString();
            GoogleLogin.instance.SavePlayerDataToFirebase();

        }

        public static void ScoreReducer(int valueToRed)
        {
            var target = UiManager.TotalCoins - valueToRed;
            if (target <= 0)
                target = 0;

            UiManager.TotalCoins = target;
            UiManager.Instance.allCanvas.persistent.coinTxt.text = UiManager.TotalCoins.ToString();
        }


        public static void GetHighScore(GameObject highScoreObj, bool isGameOver)
        {
            if (UiManager.TotalCoins >= PlayerPrefs.GetInt("HighCoins"))
            {
                PlayerPrefs.SetInt("HighCoins", UiManager.TotalCoins);
                highScoreObj.SetActive(!isGameOver);
            }
            else
            {
                highScoreObj.SetActive(false);
            }
        }

        #endregion




        #region Animation Related Methods

        public static void PlayRealAnim(List<Animation> allAnim, ref int score)
        {
            if (allAnim.Count == 0)
                return;

            Animation currentAnim = GetAnimFromList(allAnim);

            if (currentAnim)
                currentAnim.Play();

            ScoreAdder(15);

            if (currentAnim.transform.childCount <= 0)
                return;

            Text message = currentAnim.transform.GetChild(0).GetComponent<Text>();
            if (message == null)
                return;
            message.text = "Head Hit!\n +15 Coins";
        }

        public static void PlayRealAnim(List<Animation> allAnim, Vector3 targetPos)
        {
            if (allAnim.Count == 0)
                return;

            Animation currentAnim = GetAnimFromList(allAnim);
            currentAnim.transform.position = targetPos;
            if (currentAnim)
                currentAnim.Play();
        }

        public static void PlayRealAnim(List<Animation> allAnim)
        {
            if (allAnim.Count == 0)
                return;

            Animation currentAnim = GetAnimFromList(allAnim);
            if (currentAnim)
                currentAnim.Play();
        }


        public static Animation GetAnimFromList(List<Animation> anims)
        {
            Animation anim = anims[0];
            foreach (var t in anims)
            {
                if (!t.isPlaying)
                {
                    anim = t;
                }
            }

            return anim;
        }

        #endregion


        #region Camera & Screen Methods

        static Vector3 _lastPlacedObjectLocation;

        public static void SpawnAtFixedDistances(Vector3 mousePosition, bool mouseHoldStatus)
        {
            if (!mouseHoldStatus) return;
            Vector3 currentMousePosition = mousePosition;

            //now, we find how far the mouse has moved since the last step.
            Vector3 distanceTravelled = _lastPlacedObjectLocation - currentMousePosition;

            //since we know the starting and ending point, all we have to do now is add the prefab instances, neatly spaced over the travelled distance.
            Vector3 stepDistance = distanceTravelled / 0.1f; //Spacing variable default 0.1f

            //then, we interpolate between the start and end vector
            //you could make this a lot faster by determining how often your object fits in this space, but i'd like to leave you a little bit of a challenge, so i'm abusing the magnitude of the stepDistance ;)
            for (float i = 0; i < stepDistance.magnitude; i += 0.1f)
            {
                var progress = i / stepDistance.magnitude;
                Vector3 placementPosition = Vector3.Lerp(_lastPlacedObjectLocation, currentMousePosition, progress);
                // placePrefab(placementPosition);
            }


            //and then we update the last-placed-object location
            _lastPlacedObjectLocation = mousePosition;
        }

        public static void CheckOverlapWithCamera(ref List<MeshRenderer> renderers, Material original,
            Material transparent, Vector3 position, LayerMask mask)
        {
            var cl = Physics.OverlapSphere(position, 7.0f, mask);
            if (cl.Length > 0)
            {
                foreach (Collider t in cl)
                {
                    t.GetComponent<MeshRenderer>().sharedMaterial = transparent;
                    ListAdds(ref renderers, t.GetComponent<MeshRenderer>());
                }
            }

            var cls = cl.ToList();
            for (int i = 0; i < cls.Count; i++)
            {
                if (renderers.Contains(cls[i].GetComponent<MeshRenderer>())) continue;
                renderers[i].sharedMaterial = original;
                ListRemoves(ref renderers, renderers[i]);
            }
        }

        public static Transform GetClosestActiveDoll(List<Transform> ddList, Vector3 fromThis)
        {
            Transform bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = fromThis;
            for (var index = 0; index < ddList.Count; index++)
            {
                Transform potentialTarget = ddList[index];
                Vector3 directionToTarget = potentialTarget.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr && potentialTarget.gameObject.activeInHierarchy)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = potentialTarget;
                }
            }

            return bestTarget;
        }

        public static Vector3 SetHandlePositionScreenSpace(float planeDistance, Camera canvasCamera)
        {
            Vector3 screenPoint = Input.mousePosition;
            screenPoint.z = planeDistance; //distance of the plane from the camera
            return canvasCamera.ScreenToWorldPoint(screenPoint);
        }

        public static Vector3 SetHandlePositionScreenSpace(RectTransform screenPos, float planeDistance,
            Camera canvasCamera)
        {
            Vector3 pos = RectTransformUtility.WorldToScreenPoint(canvasCamera, screenPos.transform.position);
            Vector3 screenPoint = pos;
            screenPoint.z = planeDistance; //distance of the plane from the camera
            return canvasCamera.ScreenToWorldPoint(screenPoint);
        }

        public static Rect RectTransformToScreenSpace(RectTransform transform)
        {
            Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
            return new Rect((Vector2)transform.position - (size * transform.pivot), size);
        }
        #endregion

        #endregion

        #region Wait Related Extensions

        public static void Wait(this MonoBehaviour mono, float delay, UnityAction action)
        {
            mono.StartCoroutine(ExecuteAction(delay, action));
        }

        private static IEnumerator ExecuteAction(float delay, UnityAction action)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(delay);
            action.Invoke();
        }

        public static void ExtensionCall(this MonoBehaviour mono, UnityAction action)
        {
            action?.Invoke();
        }

        #endregion
    }
}