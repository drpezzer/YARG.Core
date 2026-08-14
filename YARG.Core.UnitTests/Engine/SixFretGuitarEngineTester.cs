using NUnit.Framework;
using YARG.Core.Chart;
using YARG.Core.Engine;
using YARG.Core.Engine.Guitar;
using YARG.Core.Engine.Guitar.Engines;

namespace YARG.Core.UnitTests.Engine;

public class SixFretGuitarEngineTester : EngineTester
{
    [Test]
    public void SingleWhite3Note_IsHittableWhenHeld()
    {
        var note = NoteFor(SixFretGuitarFret.White3);
        var (engine, _) = CreateEngine(note);

        Assert.That(engine.CheckCanNoteBeHit(note, 1 << 5), Is.True);
    }

    [Test]
    public void Black1White3Chord_IsHittableWhenBothHeld()
    {
        // Black1 + White3 is the widest possible chord shape (lowest and highest fret at
        // once) - the case that would break if the chord anchor scan didn't cover Fret6.
        var chord = NoteFor(SixFretGuitarFret.Black1);
        chord.AddChildNote(NoteFor(SixFretGuitarFret.White3));

        var (engine, _) = CreateEngine(chord);

        Assert.That(engine.CheckCanNoteBeHit(chord, (1 << 0) | (1 << 5)), Is.True);
    }

    [Test]
    public void Black1White3Chord_IsNotHittableWithOnlyOneFretHeld()
    {
        var chord = NoteFor(SixFretGuitarFret.Black1);
        chord.AddChildNote(NoteFor(SixFretGuitarFret.White3));

        var (engine, _) = CreateEngine(chord);

        Assert.That(engine.CheckCanNoteBeHit(chord, 1 << 0), Is.False);
    }

    private static GuitarNote NoteFor(SixFretGuitarFret fret)
    {
        return new GuitarNote(fret, GuitarNoteType.Strum, GuitarNoteFlags.None, NoteFlags.None, 0.0, 0, 0, 0);
    }

    private static (TestSixFretGuitarEngine Engine, InstrumentDifficulty<GuitarNote> Notes) CreateEngine(GuitarNote note)
    {
        var notes = new InstrumentDifficulty<GuitarNote>(Instrument.SixFretGuitar, Difficulty.Expert,
            new() { note }, new(), new());

        var syncTrack = new SyncTrack(480);
        syncTrack.Tempos.Add(new TempoChange(120, 0, 0));

        var engineParams = new GuitarEngineParameters(
            new HitWindowSettings(0.1, 0.1, 1.0, false, 0, 1.0, 1.0, 0.15, 0.25),
            4,
            0,
            0,
            StarMultiplierThresholds,
            SoloBonusStarMultiplierThresholds,
            0.1,
            0.1,
            0.1,
            false,
            true,
            false,
            false,
            true);

        return (new TestSixFretGuitarEngine(notes, syncTrack, engineParams), notes);
    }

    private sealed class TestSixFretGuitarEngine(
        InstrumentDifficulty<GuitarNote> chart,
        SyncTrack syncTrack,
        GuitarEngineParameters engineParameters)
        : YargSixFretGuitarEngine(chart, syncTrack, engineParameters, false)
    {
        public bool CheckCanNoteBeHit(GuitarNote note, int buttonMask)
        {
            EffectiveButtonMask = (byte) buttonMask;
            return CanNoteBeHit(note);
        }
    }
}
