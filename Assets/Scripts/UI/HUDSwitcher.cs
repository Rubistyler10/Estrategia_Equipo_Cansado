using UnityEngine;

public class HUDSwitcher : MonoBehaviour
{
    [Header("Interfaces")]
    public GameObject hudJugar;
    public GameObject hudReclutarUnidad;

    private void Start()
    {
        MostrarHUDJugar();
    }

    public void MostrarHUDReclutar()
    {
        if (hudJugar != null)
            hudJugar.SetActive(false);

        if (hudReclutarUnidad != null)
            hudReclutarUnidad.SetActive(true);
    }

    public void MostrarHUDJugar()
    {
        if (hudReclutarUnidad != null)
            hudReclutarUnidad.SetActive(false);

        if (hudJugar != null)
            hudJugar.SetActive(true);
    }
}
