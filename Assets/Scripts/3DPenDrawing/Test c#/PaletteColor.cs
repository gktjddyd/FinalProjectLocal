using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PaletteColorType { Red, Green, Orange, Blue }
//편하게 하기위해 만들었읍니다...
public class PaletteColor : MonoBehaviour
{
    [SerializeField] 
    public PaletteColorType Pcolor;
}
