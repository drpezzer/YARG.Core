using NUnit.Framework;
using YARG.Core.Chart;

namespace YARG.Core.UnitTests.Chart;

public class SixToFiveFretConverterTests
{
    private static readonly SixFretGuitarFret[] Lanes =
    {
        SixFretGuitarFret.Black1, SixFretGuitarFret.Black2, SixFretGuitarFret.Black3,
        SixFretGuitarFret.White1, SixFretGuitarFret.White2, SixFretGuitarFret.White3,
    };

    // Normal five-fret charts keep Orange for Hard and up, so a converted chart must too -
    // Beginner included, which a list of named difficulties once let through.
    [TestCase(Difficulty.Beginner, FiveFretGuitarFret.Blue)]
    [TestCase(Difficulty.Easy, FiveFretGuitarFret.Blue)]
    [TestCase(Difficulty.Medium, FiveFretGuitarFret.Blue)]
    [TestCase(Difficulty.Hard, FiveFretGuitarFret.Orange)]
    [TestCase(Difficulty.Expert, FiveFretGuitarFret.Orange)]
    [TestCase(Difficulty.ExpertPlus, FiveFretGuitarFret.Orange)]
    public void Convert_HoldsLowerDifficultiesToFourLanes(Difficulty difficulty, FiveFretGuitarFret highest)
    {
        var notes = new List<GuitarNote>();
        uint tick = 0;

        // Every lane as a single note...
        foreach (var lane in Lanes)
        {
            notes.Add(Note(lane, tick));
            tick += 480;
        }

        // ...and every lane in one chord, to catch the children too
        var chord = Note(Lanes[0], tick);
        foreach (var lane in Lanes[1..])
        {
            chord.AddChildNote(Note(lane, tick));
        }
        notes.Add(chord);

        var sixFret = new InstrumentDifficulty<GuitarNote>(Instrument.SixFretGuitar, difficulty,
            notes, new List<Phrase>(), new List<TextEvent>());

        var converted = SixToFiveFretConverter.Convert(sixFret, Instrument.FiveFretGuitar);

        var frets = converted.Notes
            .SelectMany(note => note.ChildNotes.Prepend(note))
            .Select(note => note.Fret)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(frets, Has.All.LessThanOrEqualTo((int) highest));
            Assert.That(frets, Has.Some.EqualTo((int) highest), "the top allowed lane should still be used");
            Assert.That(converted.Notes, Has.Count.EqualTo(notes.Count));
        });
    }

    [Test]
    public void ConvertFret_LeavesOpenNotesAlone()
    {
        Assert.That(SixToFiveFretConverter.ConvertFret((int) SixFretGuitarFret.Open, FiveFretGuitarFret.Blue),
            Is.EqualTo((int) FiveFretGuitarFret.Open));
    }

    private static GuitarNote Note(SixFretGuitarFret fret, uint tick)
    {
        return new GuitarNote(fret, GuitarNoteType.Strum, GuitarNoteFlags.None, NoteFlags.None,
            tick / 480.0, 0, tick, 0);
    }
}
