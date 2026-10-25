namespace Grafika_komputerowa.UI;

public sealed class DocumentationForm : Form
{
    public DocumentationForm()
    {
        Text = "Dokumentacja";
        Size = new Size(640, 560);
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Segoe UI", 10.5f),
            Text = Doc
        });
    }

    private const string Doc = """
        OBSŁUGA

        Lewy przycisk myszy
        • wierzchołek / punkt kontrolny — przeciąganie (element jedzie za kursorem)
        • wnętrze wielokąta — przesunięcie całego wielokąta
        • ikona ograniczenia — usunięcie tej relacji

        Prawy przycisk myszy — menu
        • na krawędzi: odcinek / Bézier / łuk, wierzchołek w środku, długość, prostopadłość, usunięcie relacji
        • na wierzchołku: usunięcie, ciągłość G0 / G1 / C1 (przy łuku tylko G0/G1)
        • na ikonie ograniczenia: usunięcie

        Prostopadłość: PPM na pierwszej krawędzi → „Prostopadłość…” → LPM na drugiej krawędzi (może być z innego wielokąta). Esc anuluje.

        Radio u góry: rysowanie odcinków Bresenhamem albo funkcją biblioteczną.

        F1 — ta pomoc.


        ZAŁOŻENIA
        • Dwa trójkąty na starcie; kolejne wielokąty dodaje się w DemoScene.cs jedną linią AddPolygon.
        • Samoprzecięcia są dozwolone.
        • Domyślna ciągłość po włączeniu Béziera: C1 (chyba że użytkownik ustawił inną).
        • Przy obu końcach łuku G0 promień = połowa cięciwy. Tylko jeden koniec łuku może mieć G1.
        • Na krawędzi może być kilka relacji. Dodanie/usunięcie wierzchołka kasuje relacje tej krawędzi.


        MODEL
        SceneModel → lista Poly + lista Constraint.
        Poly: wierzchołki (pozycja, ciągłość) i krawędzie i → i+1 (odcinek / Bézier / łuk).
        Bézier: dwa punkty kontrolne. Łuk: promień i strona.
        Constraint: LengthConstraint albo PairConstraint (prostopadłość).


        ALGORYTMY
        Odcinek: Bresenham albo GDI+ DrawLine.
        Bézier: baza potęgowa, potem różnice w przód (kolejne punkty bez powtarzania dwumianu).
        Relacje: kilka iteracji — najpierw długość i prostopadłość, potem ciągłość G1/C1 i dopasowanie łuku. Ruch punktu kontrolnego nie rusza wierzchołków własnego segmentu, chyba że wymaga tego relacja.
        """;
}
