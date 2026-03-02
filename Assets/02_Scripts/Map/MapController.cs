using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] MapView _mapView;

    public MapView MapView => _mapView;

    public void Initialize(PortalController pc, Character player, SquadController sc)
    {
        _mapView.Initialize(pc, player, sc);
    }

    public void RequestToggleMap()
    {
        _mapView.ToggleMap();
    }

    public void RequestScrollMap(Vector2 input)
    {
        _mapView.ScrollZoom(input);
    }
}