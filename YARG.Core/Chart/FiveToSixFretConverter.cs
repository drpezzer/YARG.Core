using System.Collections.Generic;
using YARG.Core.Extensions;

namespace YARG.Core.Chart
{
    /// <summary>
    /// Converts five-fret guitar charts into six-fret (Guitar Hero Live) charts, so that
    /// six-fret players can play the vast majority of songs, which only ship a five-fret chart.
    /// </summary>
    /// <remarks>
    /// This mirrors the existing 4-lane/5-lane drums conversions: the source chart is left
    /// untouched and a converted copy is produced on load.
    /// </remarks>
    public static class FiveToSixFretConverter
    {
        /// <summary>
        /// Maps a five-fret fret value onto its six-fret equivalent.
        /// </summary>
        /// <remarks>
        /// Green/Red/Yellow/Blue/Orange land on Black1/Black2/Black3/White1/White2, i.e. the
        /// five left-most lanes of the six-fret highway, preserving the left-to-right ordering
        /// of the original chart (so anchoring, chord shapes and HOPO runs stay recognizable).
        /// White3 is intentionally left unused - a five-lane chart has no sixth note to give it,
        /// and inventing one would change the charted music.
        ///
        /// Note that <see cref="FiveFretGuitarFret"/> and <see cref="SixFretGuitarFret"/> already
        /// share the same underlying values for this mapping (Green == Black1 == 1, and so on,
        /// with Open/Wildcard identical), so this is currently an identity mapping on the raw
        /// fret number. It is written out explicitly anyway: the mapping is a gameplay design
        /// decision rather than a coincidence of the enums, and this is the single place to
        /// change it if the layout is ever retuned.
        /// </remarks>
        public static int ConvertFret(int fiveFretValue)
        {
            return (FiveFretGuitarFret) fiveFretValue switch
            {
                FiveFretGuitarFret.Green    => (int) SixFretGuitarFret.Black1,
                FiveFretGuitarFret.Red      => (int) SixFretGuitarFret.Black2,
                FiveFretGuitarFret.Yellow   => (int) SixFretGuitarFret.Black3,
                FiveFretGuitarFret.Blue     => (int) SixFretGuitarFret.White1,
                FiveFretGuitarFret.Orange   => (int) SixFretGuitarFret.White2,
                FiveFretGuitarFret.Open     => (int) SixFretGuitarFret.Open,
                FiveFretGuitarFret.Wildcard => (int) SixFretGuitarFret.Wildcard,
                // Anything unexpected is passed through untouched rather than dropped, so an
                // unusual chart degrades into something playable instead of a silent gap.
                _ => fiveFretValue,
            };
        }

        /// <summary>
        /// Gets the five-fret instrument a six-fret instrument should fall back to when the song
        /// has no six-fret chart of its own.
        /// </summary>
        public static Instrument GetFiveFretEquivalent(Instrument sixFretInstrument)
        {
            return sixFretInstrument switch
            {
                Instrument.SixFretGuitar     => Instrument.FiveFretGuitar,
                Instrument.SixFretBass       => Instrument.FiveFretBass,
                Instrument.SixFretRhythm     => Instrument.FiveFretRhythm,
                Instrument.SixFretCoopGuitar => Instrument.FiveFretCoopGuitar,
                _ => sixFretInstrument,
            };
        }

        /// <summary>
        /// Produces a six-fret copy of a five-fret difficulty.
        /// </summary>
        public static InstrumentDifficulty<GuitarNote> Convert(InstrumentDifficulty<GuitarNote> fiveFret,
            Instrument targetInstrument)
        {
            var notes = new List<GuitarNote>(fiveFret.Notes.Count);

            foreach (var sourceNote in fiveFret.Notes)
            {
                var converted = ConvertNote(sourceNote);

                // Chords are stored as a parent note plus child notes on the same tick; each
                // child has to be converted and re-attached, since AddChildNote is what rebuilds
                // the parent's combined NoteMask.
                foreach (var sourceChild in sourceNote.ChildNotes)
                {
                    converted.AddChildNote(ConvertNote(sourceChild));
                }

                notes.Add(converted);
            }

            // The engine walks Previous/Next to make HOPO and ghosting decisions, so the chain
            // has to be rebuilt across the new note objects.
            for (int i = 0; i < notes.Count; i++)
            {
                if (i > 0)
                {
                    notes[i].PreviousNote = notes[i - 1];
                }

                if (i < notes.Count - 1)
                {
                    notes[i].NextNote = notes[i + 1];
                }
            }

            // Phrases (star power, solos, lanes) and text events are fret-agnostic, so they can
            // be copied across as-is. Range shifts are dropped: they describe a five-fret
            // sub-range of the neck and have no meaning on a six-fret highway.
            return new InstrumentDifficulty<GuitarNote>(
                targetInstrument,
                fiveFret.Difficulty,
                notes,
                fiveFret.Phrases.Duplicate(),
                fiveFret.TextEvents.Duplicate());
        }

        private static GuitarNote ConvertNote(GuitarNote source)
        {
            return new GuitarNote(
                ConvertFret(source.Fret),
                source.Type,
                source.GuitarFlags,
                source.Flags,
                source.Time,
                source.TimeLength,
                source.Tick,
                source.TickLength);
        }
    }
}
