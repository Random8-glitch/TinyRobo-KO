using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine;


public class UITienda : MonoBehaviour
{
    private static UITienda self;

    private void Awake()
    {
        self = this;
        Awake_Tienda();
       
    }

    [Header("\n Tienda")]
    [SerializeField] private GameObject panelTienda;
    public static GameObject PanelTienda => self.panelTienda;

    [Header("\n Botones")]
    [SerializeField] private Button botonChips;
    [SerializeField] private Button botonCarroza;
    [SerializeField] private Button botonArmas;

    [Header("\n Paneles")]
    [SerializeField] private GameObject panelChips;
    [SerializeField] private GameObject panelCarroza;
    [SerializeField] private GameObject panelArmas;

    private List<GameObject> paneles;


    private void Awake_Tienda()
    {
       
        paneles = new List<GameObject>
        {
            panelChips,
            panelCarroza,
            panelArmas
        };
        botonChips.onClick.AddListener(() => { CambiarPanel(panelChips); });
        botonCarroza.onClick.AddListener(() => { CambiarPanel(panelCarroza); });
        botonArmas.onClick.AddListener(() => { CambiarPanel(panelArmas); });

       

    }

    private void CambiarPanel(GameObject panel)
    {
        OcultarPaneles();
        panel.SetActive(true);
    }

    private void OnEnable()

    {
       
        OcultarPaneles();
        panelTienda.SetActive(true);

        CambiarPanel(panelChips);
    }

    private void OcultarPaneles()
    {
        foreach (GameObject panel in paneles)
        {
            panel.SetActive(false);
        }
    }
}
