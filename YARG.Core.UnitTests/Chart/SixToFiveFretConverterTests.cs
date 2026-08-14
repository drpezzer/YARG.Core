using NUnit.Framework;
using YARG.Core;
using YARG.Core.Chart;

namespace YARG.Core.UnitTests.Chart;

public class SixToFiveFretConverterTests
{
    [TestCase(SixFretGuitarFret.Black1,   FiveFretGuitarFret.Green)]
    [TestCase(SixFretGuitarFret.Black2,   FiveFretGuitarFret.Red)]
    [TestCase(SixFretGuitarFret.Black3,   FiveFretGuitarFret.Yellow)]
    [TestCase(SixFretGuitarFret.White1,   FiveFretGuitarFret.Blue)]
    [TestCase(SixFretGuitarFret.White2,   FiveFretGuitarFret.Orange)]
    [TestCase(SixFretGuitarFret.White3,   FiveFretGuitarFret.Orange)]
    [TestCase(SixFretGuitarFret.Open,     FiveFretGuitarFret.Open)]
    [TestCase(SixFretGuitarFret.Wildcard, FiveFretGuitarFret.Wildcard)]
    public void ConvertFret_MapsEachFret(SixFretGuitarFret source, FiveFretGuitarFret expected)
    {
        Assert.That(SixToFiveFretConverter.ConvertFret((int) source), Is.EqualTo((int) expected));
    }

    [Test]
    public void ConvertFret_NeverProducesAFretOutsideFiveLanes()
    {
        foreach (SixFretGuitarFret fret in System.Enum.GetValues(typeof(SixFretGuitarFret)))
        {
            int converted = SixToFiveFretConverter.ConvertFret((int) fret);

            Assert.That(converted, Is.LessThanOrEqualTo((int) FiveFretGuitarFret.Wildcard),
                $"{fret} converted to a value a five-fret highway has no lane for");
        }
    }

    [Test]
    public void Convert_PreservesTimingAndType()
    {
        var source = DifficultyOf(
            Note(SixFretGuitarFret.Black1, GuitarNoteType.Strum, time: 0.0, tick: 0),
            Note(SixFretGuitarFret.White3, GuitarNoteType.Hopo, time: 1.5, tick: 720));

        var converted = SixToFiveFretConverter.Convert(source, Instrument.FiveFretGuitar);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(converted.Instrument, Is.EqualTo(Instrument.FiveFretGuitar));
            Assert.That(converted.Notes, Has.Count.EqualTo(2));

            Assert.That(converted.Notes[0].Fret, Is.EqualTo((int) FiveFretGuitarFret.Green));
            Assert.That(converted.Notes[1].Fret, Is.EqualTo((int) FiveFretGuitarFret.Orange));
            Assert.That(converted.Notes[1].Type, Is.EqualTo(GuitarNoteType.Hopo));
            Assert.That(converted.Notes[1].Tick, Is.EqualTo(720u));
        }
    }

    [Test]
    public void Convert_KeepsBothNotesOfAChordThatDoesNotCollide()
    {
        // A both-buttons GHL chord: Black1 + White1 -> Green + Blue.
        var chord = Note(SixFretGuitarFret.Black1, GuitarNoteType.Strum);
        chord.AddChildNote(Note(SixFretGuitarFret.White1, GuitarNoteType.Strum));

        var converted = SixToFiveFretConverter.Convert(DifficultyOf(chord), Instrument.FiveFretGuitar).Notes[0];

        int expectedMask = (1 << ((int) FiveFretGuitarFret.Green - 1)) |
                           (1 << ((int) FiveFretGuitarFret.Blue - 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(converted.ChildNotes, Has.Count.EqualTo(1));
            Assert.That(converted.NoteMask, Is.EqualTo(expectedMask));
        }
    }

    [Test]
    public void Convert_CollapsesTheCollidingChordIntoOneNote()
    {
        // White2 and White3 are the pair that has to share a lane; the chord must become a
        // single Orange note rather than two notes stacked on the same lane.
        var chord = Note(SixFretGuitarFret.White2, GuitarNoteType.Strum);
        chord.AddChildNote(Note(SixFretGuitarFret.White3, GuitarNoteType.Strum));

        var converted = SixToFiveFretConverter.Convert(DifficultyOf(chord), Instrument.FiveFretGuitar).Notes[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(converted.Fret, Is.EqualTo((int) FiveFretGuitarFret.Orange));
            Assert.That(converted.ChildNotes, Is.Empty);
            Assert.That(converted.NoteMask, Is.EqualTo(1 << ((int) FiveFretGuitarFret.Orange - 1)));
        }
    }

    [Test]
    public void Convert_PreservesSustainLength()
    {
        var sustain = Note(SixFretGuitarFret.Black3, GuitarNoteType.Strum, timeLength: 2.25, tickLength: 960);

        var converted = SixToFiveFretConverter.Convert(DifficultyOf(sustain), Instrument.FiveFretGuitar).Notes[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(converted.TimeLength, Is.EqualTo(2.25));
            Assert.That(converted.TickLength, Is.EqualTo(960u));
            Assert.That(converted.IsSustain, Is.True);
        }
    }

    [Test]
    public void Convert_RebuildsPreviousNextChainOntoConvertedNotes()
    {
        var converted = SixToFiveFretConverter.Convert(DifficultyOf(
            Note(SixFretGuitarFret.Black1, GuitarNoteType.Strum, time: 0.0, tick: 0),
            Note(SixFretGuitarFret.Black2, GuitarNoteType.Strum, time: 1.0, tick: 480),
            Note(SixFretGuitarFret.White1, GuitarNoteType.Strum, time: 2.0, tick: 960)),
            Instrument.FiveFretGuitar);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(converted.Notes[0].PreviousNote, Is.Null);
            Assert.That(converted.Notes[1].PreviousNote, Is.SameAs(converted.Notes[0]));
            Assert.That(converted.Notes[1].NextNote, Is.SameAs(converted.Notes[2]));
            Assert.That(converted.Notes[2].NextNote, Is.Null);
        }
    }

    [Test]
    public void Convert_DoesNotMutateSourceChart()
    {
        var source = DifficultyOf(Note(SixFretGuitarFret.White3, GuitarNoteType.Strum));

        SixToFiveFretConverter.Convert(source, Instrument.FiveFretGuitar);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(source.Instrument, Is.EqualTo(Instrument.SixFretGuitar));
            Assert.That(source.Notes[0].Fret, Is.EqualTo((int) SixFretGuitarFret.White3));
        }
    }

    [Test]
    public void RoundTrip_ThroughSixFretAndBackIsLossless()
    {
        // Five-fret charts never use White3, so converting one to six-fret and back must return
        // exactly the original frets - the collapse only ever affects genuine GHL charts.
        var frets = new[]
        {
            FiveFretGuitarFret.Green, FiveFretGuitarFret.Red, FiveFretGuitarFret.Yellow,
            FiveFretGuitarFret.Blue, FiveFretGuitarFret.Orange, FiveFretGuitarFret.Open,
        };

        foreach (var fret in frets)
        {
            int toSix = FiveToSixFretConverter.ConvertFret((int) fret);
            int backToFive = SixToFiveFretConverter.ConvertFret(toSix);

            Assert.That(backToFive, Is.EqualTo((int) fret), $"{fret} did not survive the round trip");
        }
    }

    private static GuitarNote Note(SixFretGuitarFret fret, GuitarNoteType type,
        double time = 0.0, uint tick = 0, double timeLength = 0, uint tickLength = 0)
    {
        return new GuitarNote(fret, type, GuitarNoteFlags.None, NoteFlags.None,
            time, timeLength, tick, tickLength);
    }

    private static InstrumentDifficulty<GuitarNote> DifficultyOf(params GuitarNote[] notes)
    {
        return new InstrumentDifficulty<GuitarNote>(Instrument.SixFretGuitar, Difficulty.Expert,
            new(notes), new(), new());
    }
}
