using System.Collections.Generic;
using YARG.Core.Extensions;

namespace YARG.Core.Chart
{
    /// <summary>
    /// Converts six-fret (Guitar Hero Live) charts into five-fret charts, so songs that only
    /// ship a GHL chart can still be played on a normal five-fret guitar.
    /// </summary>
    /// <remarks>
    /// The mirror of <see cref="FiveToSixFretConverter"/>. Together they mean every song in a
    /// library is playable on either kind of guitar, whichever way round it was charted.
    /// </remarks>
    public static class SixToFiveFretConverter
    {
        /// <summary>
        /// The highest five-fret lane a difficulty is allowed to use.
        /// </summary>
        /// <remarks>
        /// Five-fret charts traditionally hold Medium and below to four lanes, keeping Orange for
        /// Hard and Expert. A six-fret chart has no such convention - its lower difficulties still
        /// use both rows - so converting one straight across puts Orange notes on Medium, which
        /// reads as wrong to anyone used to normal charts and asks for a stretch the difficulty
        /// never intends.
        /// </remarks>
        private static FiveFretGuitarFret HighestLane(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy or Difficulty.Medium => FiveFretGuitarFret.Blue,
                _                                    => FiveFretGuitarFret.Orange,
            };
        }

        /// <summary>
        /// Maps a six-fret fret value onto its five-fret equivalent.
        /// </summary>
        /// <remarks>
        /// Six frets don't fit into five lanes, so exactly one pair has to share. The order is
        /// kept ascending (Black1..White3 -> Green..Orange) so runs still travel across the
        /// neck the same way, and White3 - the top fret, and the one a converted five-fret chart
        /// never produces - is the one folded onto Orange.
        ///
        /// A chord containing both White2 and White3 therefore collapses to a single Orange
        /// note. That's handled for free: <see cref="GuitarNote.AddChildNote"/> ignores a child
        /// whose fret is already in the chord's mask, so the duplicate is dropped rather than
        /// producing two notes on one lane.
        /// </remarks>
        public static int ConvertFret(int sixFretValue)
        {
            return ConvertFret(sixFretValue, FiveFretGuitarFret.Orange);
        }

        /// <summary>
        /// Maps a six-fret fret value onto its five-fret equivalent, folding anything above
        /// <paramref name="highestLane"/> down onto it.
        /// </summary>
        public static int ConvertFret(int sixFretValue, FiveFretGuitarFret highestLane)
        {
            int converted = (SixFretGuitarFret) sixFretValue switch
            {
                SixFretGuitarFret.Black1   => (int) FiveFretGuitarFret.Green,
                SixFretGuitarFret.Black2   => (int) FiveFretGuitarFret.Red,
                SixFretGuitarFret.Black3   => (int) FiveFretGuitarFret.Yellow,
                SixFretGuitarFret.White1   => (int) FiveFretGuitarFret.Blue,
                SixFretGuitarFret.White2   => (int) FiveFretGuitarFret.Orange,
                SixFretGuitarFret.White3   => (int) FiveFretGuitarFret.Orange,
                SixFretGuitarFret.Open     => (int) FiveFretGuitarFret.Open,
                SixFretGuitarFret.Wildcard => (int) FiveFretGuitarFret.Wildcard,
                // Anything unexpected passes through untouched rather than being dropped.
                _ => sixFretValue,
            };

            // Open and wildcard aren't lanes and must not be clamped.
            if (converted is (int) FiveFretGuitarFret.Open or (int) FiveFretGuitarFret.Wildcard)
            {
                return converted;
            }

            return converted > (int) highestLane ? (int) highestLane : converted;
        }

        /// <summary>
        /// Gets the six-fret instrument a five-fret instrument should fall back to when the song
        /// has no five-fret chart of its own.
        /// </summary>
        public static Instrument GetSixFretEquivalent(Instrument fiveFretInstrument)
        {
            return fiveFretInstrument switch
            {
                Instrument.FiveFretGuitar     => Instrument.SixFretGuitar,
                Instrument.FiveFretBass       => Instrument.SixFretBass,
                Instrument.FiveFretRhythm     => Instrument.SixFretRhythm,
                Instrument.FiveFretCoopGuitar => Instrument.SixFretCoopGuitar,
                _ => fiveFretInstrument,
            };
        }

        /// <summary>
        /// Produces a five-fret copy of a six-fret difficulty.
        /// </summary>
        public static InstrumentDifficulty<GuitarNote> Convert(InstrumentDifficulty<GuitarNote> sixFret,
            Instrument targetInstrument)
        {
            var notes = new List<GuitarNote>(sixFret.Notes.Count);

            var highestLane = HighestLane(sixFret.Difficulty);

            foreach (var sourceNote in sixFret.Notes)
            {
                var converted = ConvertNote(sourceNote, highestLane);

                foreach (var sourceChild in sourceNote.ChildNotes)
                {
                    // Duplicates (a collision on the top lane) are dropped by AddChildNote.
                    converted.AddChildNote(ConvertNote(sourceChild, highestLane));
                }

                notes.Add(converted);
            }

            // The engine walks Previous/Next for HOPO and ghosting decisions, so the chain has to
            // be rebuilt across the new note objects.
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

            return new InstrumentDifficulty<GuitarNote>(
                targetInstrument,
                sixFret.Difficulty,
                notes,
                sixFret.Phrases.Duplicate(),
                sixFret.TextEvents.Duplicate());
        }

        private static GuitarNote ConvertNote(GuitarNote source, FiveFretGuitarFret highestLane)
        {
            return new GuitarNote(
                ConvertFret(source.Fret, highestLane),
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
