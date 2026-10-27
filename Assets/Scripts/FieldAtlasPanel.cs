using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        void Map()
        {
            Rect(365, 29, 850, 850, glass); float span = RoundArena.Size / mapZoom; var bounds = new Rect(390, 68, 800, 800); var ev = Event.current;
            if (bounds.Contains(ev.mousePosition) && ev.type == EventType.ScrollWheel)
            {
                float old = span; Vector2 uv = new Vector2((ev.mousePosition.x - bounds.x) / bounds.width - .5f, .5f - (ev.mousePosition.y - bounds.y) / bounds.height);
                mapZoom = Mathf.Clamp(mapZoom * Mathf.Pow(1.18f, -ev.delta.y), 1, 16); span = RoundArena.Size / mapZoom; mapCenter += uv * (old - span); ev.Use();
            }
            if (ev.type == EventType.MouseDown && ev.button == 0 && bounds.Contains(ev.mousePosition)) dragging = true; if (ev.type == EventType.MouseUp) dragging = false;
            if (dragging && ev.type == EventType.MouseDrag) { mapCenter += new Vector2(-ev.delta.x, ev.delta.y) * span / bounds.width; ev.Use(); }
            float limit = (RoundArena.Size - span) / 2; mapCenter.x = Mathf.Clamp(mapCenter.x, -limit, limit); mapCenter.y = Mathf.Clamp(mapCenter.y, -limit, limit);
            DrawTactical(bounds, mapCenter, span, true); Text(391, 32, 780, 29, "Pinefall reserve   /   "+RoundArena.Label+"   /   North ↑", small, 19, paper);
            Rect(1240, 68, 324, 654, glass); Text(1260, 88, 285, 48, "Map features", type, 28, paper);
            string[] labels = { "You / heading", "Teammates", "Nearby wildlife", "Supply drop", "Current ring", "Next safe zone", "Cars / motorcycles", "Motor launches", "Launchpads", "Cave passages" };
            string[] icons = { "sprint", "shield", "paw", "crate", "ring", "ring", "car", "boat", "launchpad", "cave" };
            Color[] colors = { paper, ally, prey, ice, shieldColor, gold, ice, ice, ice, gold };
            for (int i = 0; i < labels.Length; i++) { Glyph(1261, 157 + i * 39, 23, icons[i], colors[i]); Text(1298, 155 + i * 39, 240, 32, labels[i], small, 18, paper); }
            if (P)
            {
                Vector3 place = P.ObserverPosition;
                Text(1260, 555, 283, 50, ReserveBiomes.Label(place.x, place.z), type, 23, gold);
                Text(1260, 615, 283, 76, ExpeditionLayout.Regions[ExpeditionLayout.Nearest(place)].name + (P.SurveyVisible ? "\nSurvey snapshots active" : "\nEnemies hidden"), small, 18, paper);
            }
            if (Button(1240, 742, 155, 45, "− Zoom")) mapZoom = Mathf.Max(1, mapZoom / 1.5f);
            if (Button(1409, 742, 155, 45, "+ Zoom")) mapZoom = Mathf.Min(16, mapZoom * 1.5f);
            if (Button(1240, 799, 324, 46, "Center on me")) { if (P) mapCenter = new Vector2(P.ObserverPosition.x, P.ObserverPosition.z); mapZoom = Mathf.Max(4, mapZoom); }
            Text(30, 35, 310, 40, "Reserve map", type, 30, paper); Text(30, 100, 300, 82, "Scroll to zoom\nDrag to pan\nHover icons for details", small, 19, paper);
            if (Button(30, 802, 292, 50, "Return to field", true)) ClosePanel();
        }
    }
}
