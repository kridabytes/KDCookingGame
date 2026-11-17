using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiTechStudio.Cooking2D
{
    public class OrderBotDish : MonoBehaviour
    {

        private LevelController levelController;
        public int orderNo;
        public int subOrderNo = 0;

        [SerializeField]
        private GameObject childImg;

        private bool setServed;

        [Header("Cupcake Image")]
        public Sprite cupcakeImg;
        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetServed()
        {
            if (setServed)
                return;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
            transform.GetComponent<Image>().sprite = GameManager.instance.tickImg;
            setServed = true;
            this.Wait(1, () => gameObject.SetActive(false));
        }

        public void SetData()
        {

            levelController = LevelManager.Instance.ThisLevel;

            GameObject _imgGo = Instantiate(childImg, childImg.transform.position, childImg.transform.rotation, childImg.transform.parent);
            if (subOrderNo <= 0)
            {
                // _imgGo.GetComponent<Image>().sprite = levelController.FindImageAsLevel(levelController.allOrders[orderNo].coockBase);
                if (levelController.allOrders[orderNo].coockBase == ObjectType.Pizza)
                {
                    Sprite finalPizza = levelController.GetFinalPizzaSprite();

                    if (finalPizza != null)
                    {
                        _imgGo.GetComponent<Image>().sprite = finalPizza;
                    }
                }
                else if (levelController.allOrders[orderNo].coockBase == ObjectType.GarlicBread)
                {
                    if (levelController.garlicBreadManager != null)
                    {
                        _imgGo.GetComponent<Image>().sprite =
                            levelController.garlicBreadManager.GetGarlicBreadSprite();
                    }
                }
                else if (levelController.allOrders[orderNo].coockBase == ObjectType.CapCake)
                {
                    _imgGo.GetComponent<Image>().sprite = cupcakeImg;
                }
                else
                {
                    _imgGo.GetComponent<Image>().sprite = levelController.FindImageAsLevel(levelController.allOrders[orderNo].coockBase);
                }


                _imgGo.transform.SetParent(transform);
                _imgGo.SetActive(true);
                if (levelController.allOrders[orderNo].coockBase == ObjectType.JuiceMachine1)
                {
                    // transform.GetComponent<Image>().sprite = null;
                    transform.GetComponent<Image>().enabled = false;
                }

                for (int i = 0; i < levelController.allOrders[orderNo].allToppings.Length; i++)
                {
                    GameObject _imgGo2 = Instantiate(childImg, childImg.transform.position, childImg.transform.rotation, childImg.transform.parent);
                    _imgGo2.GetComponent<Image>().sprite = levelController.FindImageAsLevel(levelController.allOrders[orderNo].allToppings[i]);
                    _imgGo2.transform.SetParent(transform);
                    _imgGo2.SetActive(true);
                }
            }
            else
            {
                // _imgGo.GetComponent<Image>().sprite = levelController.FindImageAsLevel(levelController.allOrders[orderNo].allMultiOrders[subOrderNo-1].coockBase);
                ObjectType type = levelController.allOrders[orderNo].allMultiOrders[subOrderNo - 1].coockBase;

                if (type == ObjectType.Pizza)
                {
                    Sprite finalPizza = levelController.GetFinalPizzaSprite();

                    if (finalPizza != null)
                    {
                        _imgGo.GetComponent<Image>().sprite = finalPizza;
                    }
                }
                else if (type == ObjectType.GarlicBread)
                {
                    _imgGo.GetComponent<Image>().sprite =
                        levelController.garlicBreadManager.GetGarlicBreadSprite();
                }
                else if (type == ObjectType.CapCake)
                {
                    _imgGo.GetComponent<Image>().sprite = cupcakeImg;
                }
                else
                {
                    _imgGo.GetComponent<Image>().sprite =
                        levelController.FindImageAsLevel(type);
                }
                _imgGo.transform.SetParent(transform);
                _imgGo.SetActive(true);
                if (levelController.allOrders[orderNo].allMultiOrders[subOrderNo - 1].coockBase == ObjectType.JuiceMachine1)
                {
                    // transform.GetComponent<Image>().sprite = null;
                    transform.GetComponent<Image>().enabled = false;
                }

                for (int i = 0; i < levelController.allOrders[orderNo].allMultiOrders[subOrderNo - 1].allToppings.Length; i++)
                {
                    GameObject _imgGo2 = Instantiate(childImg, childImg.transform.position, childImg.transform.rotation, childImg.transform.parent);
                    _imgGo2.GetComponent<Image>().sprite = levelController.FindImageAsLevel(levelController.allOrders[orderNo].allMultiOrders[subOrderNo - 1].allToppings[i]);
                    _imgGo2.transform.SetParent(transform);
                    _imgGo2.SetActive(true);
                }
            }
            gameObject.SetActive(true);
        }
    }
}
