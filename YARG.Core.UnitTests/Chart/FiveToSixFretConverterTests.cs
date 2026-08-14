using NUnit.Framework;
using YARG.Core;
using YARG.Core.Chart;

namespace YARG.Core.UnitTests.Chart;

public class FiveToSixFretConverterTests
{
    [TestCase(FiveFretGuitarFret.Green,    SixFretGuitarFret.Black1)]
    [TestCase(FiveFretGuitarFret.Red,      SixFretGuitarFret.Black2)]
    [TestCase(FiveFretGuitarFret.Yellow,   SixFretGuitarFret.Black3)]
    [TestCase(FiveFretGuitarFret.Blue,     SixFretGuitarFret.White1)]
    [TestCase(FiveFretGuitarFret.Orange,   SixFretGuitarFret.White2)]
    [TestCase(FiveFretGuitarFret.Open,     SixFretGuitarFret.Open)]
    [TestCase(FiveFretGuitarFret.Wildcard, SixFretGuitarFret.Wildcard)]
    public void ConvertFret_MapsEachLane(FiveFretGuitarFret source, SixFretGuitarFret expected)
    {
        Assert.That(FiveToSixFretConverter.ConvertFret((int) source), Is.EqualTo((int) expected));
    }

    [Test]
    public void Convert_PreservesNoteCountTimingAndType()
    {
        var source = DifficultyOf(
            Note(FiveFretGuitarFret.Green, GuitarNoteType.Strum, time: 0.0, tick: 0),
            Note(FiveFretGuitarFret.Orange, GuitarNoteType.Hopo, time: 1.5, tick: 720));

        var converted = FiveToSixFretConverter.Convert(source, Instrument.SixFretGuitar);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(converted.Instrument, Is.EqualTo(Instrument.SixFretGuitar));
            Assert.That(converted.Notes, Has.Count.EqualTo(2));

            Assert.That(converted.Notes[0].Fret, Is.EqualTo((int) SixFretGuitarFret.Black1));
            Assert.That(converted.Notes[0].Type, Is.EqualTo(GuitarNoteType.Strum));
            Assert.That(converted.Notes[0].Time, Is.EqualTo(0.0));

            Assert.That(converted.Notes[1].Fret, Is.EqualTo((int) SixFretGuitarFret.White2));
            Assert.That(converted.Notes[1].Type, Is.EqualTo(GuitarNoteType.Hopo));
            Assert.That(converted.Notes[1].Time, Is.EqualTo(1.5));
            Assert.That(converted.Notes[1].Tick, Is.EqualTo(720u));
        }
    }

    [Test]
    public void Convert_RemapsChordChildNotesAndCombinedMask()
    {
        // Green + Orange is the widest five-fret chord; it should land on Black1 + White2.
        var chord = Note(FiveFretGuitarFret.Green, GuitarNoteType.Strum);
        chord.AddChildNote(Note(FiveFretGuitarFret.Orange, GuitarNoteType.Strum));

        var converted = FiveToSixFretConverter.Convert(DifficultyOf(chord), Instrument.SixFretGuitar);
        var convertedChord = converted.Notes[0];

        int expectedMask = (1 << ((int) SixFretGuitarFret.Black1 - 1)) |
                           (1 << ((int) SixFretGuitarFret.White2 - 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(convertedChord.ChildNotes, Has.Count.EqualTo(1));
            Assert.That(convertedChord.Fret, Is.EqualTo((int) SixFretGuitarFret.Black1));
            Assert.That(convertedChord.ChildNotes[0].Fret, Is.EqualTo((int) SixFretGuitarFret.White2));
            Assert.That(convertedChord.NoteMask, Is.EqualTo(expectedMask));
        }
    }

    [Test]
    public void Convert_PreservesSustainLength()
    {
        var sustain = Note(FiveFretGuitarFret.Blue, GuitarNoteType.Strum, timeLength: 2.25, tickLength: 960);

        var converted = FiveToSixFretConverter.Convert(DifficultyOf(sustain), Instrument.SixFretGuitar);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(converted.Notes[0].TimeLength, Is.EqualTo(2.25));
            Assert.That(converted.Notes[0].TickLength, Is.EqualTo(960u));
            Assert.That(converted.Notes[0].IsSustain, Is.True);
        }
    }

    [Test]
    public void Convert_RebuildsPreviousNextChainOntoConvertedNotes()
    {
        var converted = FiveToSixFretConverter.Convert(DifficultyOf(
            Note(FiveFretGuitarFret.Green, GuitarNoteType.Strum, time: 0.0, tick: 0),
            Note(FiveFretGuitarFret.Red, GuitarNoteType.Strum, time: 1.0, tick: 480),
            Note(FiveFretGuitarFret.Yellow, GuitarNoteType.Strum, time: 2.0, tick: 960)),
            Instrument.SixFretGuitar);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(converted.Notes[0].PreviousNote, Is.Null);
            // Must point at the converted notes, not the original five-fret ones
            Assert.That(converted.Notes[1].PreviousNote, Is.SameAs(converted.Notes[0]));
            Assert.That(converted.Notes[1].NextNote, Is.SameAs(converted.Notes[2]));
            Assert.That(converted.Notes[2].NextNote, Is.Null);
        }
    }

    [Test]
    public void Convert_DoesNotMutateSourceChart()
    {
        var source = DifficultyOf(Note(FiveFretGuitarFret.Orange, GuitarNoteType.Strum));

        FiveToSixFretConverter.Convert(source, Instrument.SixFretGuitar);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(source.Instrument, Is.EqualTo(Instrument.FiveFretGuitar));
            Assert.That(source.Notes[0].Fret, Is.EqualTo((int) FiveFretGuitarFret.Orange));
        }
    }

    [Test]
    public void Convert_NeverProducesWhite3()
    {
        // No five-fret lane maps onto the sixth fret; a chart that grew one would mean the
        // mapping table changed without the highway/engine being reconsidered.
        var source = DifficultyOf(
            Note(FiveFretGuitarFret.Green, GuitarNoteType.Strum, time: 0.0, tick: 0),
            Note(FiveFretGuitarFret.Red, GuitarNoteType.Strum, time: 0.5, tick: 240),
            Note(FiveFretGuitarFret.Yellow, GuitarNoteType.Strum, time: 1.0, tick: 480),
            Note(FiveFretGuitarFret.Blue, GuitarNoteType.Strum, time: 1.5, tick: 720),
            Note(FiveFretGuitarFret.Orange, GuitarNoteType.Strum, time: 2.0, tick: 960),
            Note(FiveFretGuitarFret.Open, GuitarNoteType.Strum, time: 2.5, tick: 1200));

        var converted = FiveToSixFretConverter.Convert(source, Instrument.SixFretGuitar);

        Assert.That(converted.Notes.Exists(n => n.Fret == (int) SixFretGuitarFret.White3), Is.False);
    }

    private static GuitarNote Note(FiveFretGuitarFret fret, GuitarNoteType type,
        double time = 0.0, uint tick = 0, double timeLength = 0, uint tickLength = 0)
    {
        return new GuitarNote(fret, type, GuitarNoteFlags.None, NoteFlags.None,
            time, timeLength, tick, tickLength);
    }

    private static InstrumentDifficulty<GuitarNote> DifficultyOf(params GuitarNote[] notes)
    {
        return new InstrumentDifficulty<GuitarNote>(Instrument.FiveFretGuitar, Difficulty.Expert,
            new(notes), new(), new());
    }
}
