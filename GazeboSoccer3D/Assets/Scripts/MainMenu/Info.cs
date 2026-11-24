using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Info : MonoBehaviour
{
    // GameObject listesini tanımla
    public List<GameObject> trToToggle;
    public List<GameObject> enToToggle;


    // Aktiflik durumunu takip eden bayrak
    private bool isActive = false;

    // Butona her basıldığında aktif/pasif durumunu değiştiren metot
    public void ToggleObjects()
    {
        // Bayrağı tersine çevir
        isActive = !isActive;

        // Listedeki her bir obje için işlemi gerçekleştir
        foreach (GameObject obj in trToToggle)
        {
            if (obj != null) // Obje null değilse
            {
                // Aktiflik durumunu bayrağa göre ayarla
                obj.SetActive(isActive);
            }
        }

        foreach (GameObject obj in enToToggle)
        {
            if (obj != null) // Obje null değilse
            {
                // Aktiflik durumunu bayrağa göre ayarla
                obj.SetActive(isActive);
            }
        }
    }
}
