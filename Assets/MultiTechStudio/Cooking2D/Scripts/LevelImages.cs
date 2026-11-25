using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
/// <summary>
/// Holds sprites used by a level/cart: cook base states, toppings, juice fill frames, and upgrade visuals.
/// </summary>
[CreateAssetMenu(fileName = "NewLevel", menuName = "MultiTechStudio/Cooking2D/LevelImages")]
public class LevelImages : ScriptableObject
{
    public BaseCookImages[] cookBase1Images;
    public BaseCookImages[] cookBase2Images;

    public ImagesAsUpgrade[] toppingImgs;
    [Space(7)]
    public Sprite[] glass0FillImgs;
    public Sprite[] glass1FillImgs;
    public Sprite[] glass2FillImgs;
    public Sprite[] glassEmptyImgs;
    [Tooltip("Empty coffee cup for each coffee cup upgrade (truck 2 / 3).")]
    public Sprite[] coffeeEmptyImgs;
    [Space(7)]
    public ImagesAsUpgrade[] upgradeImgs;

    public BaseCookImages[] pizzaCookImages;
    public BaseCookImages[] cupcakeCookImages;
}
}
