using System.Drawing;
using System.IO;
using YARG.Core.Chart;
using YARG.Core.Extensions;
using YARG.Core.Game.Settings;
using YARG.Core.Utility;

namespace YARG.Core.Game
{
    public partial class ColorProfile
    {
        public class SixFretGuitarColors : IFretColorProvider, IBinarySerializable
        {
            // Guitar Hero Live doesn't use a per-lane rainbow: the neck has a black row and a
            // white row, and that's the only color information a note carries. Pure black would
            // disappear against the highway, so the black row is a dark slate that still reads
            // as "black" next to the white row.
            [SettingType(SettingType.Ignore)]
            private static readonly Color BlackRow = Color.FromArgb(0xFF, 0x3A, 0x3A, 0x42); // #3A3A42
            // Likewise the white row is pulled back from pure white - emission multiplies the
            // color (x3 on sustains, x11.5 on frets), so #FFFFFF blows out into a flat glare.
            [SettingType(SettingType.Ignore)]
            private static readonly Color WhiteRow = Color.FromArgb(0xFF, 0xC8, 0xC8, 0xD2); // #C8C8D2

            // An unlit fret is drawn dimmer than a note, so the black row's fret is lifted a
            // little to stay visible against the highway.
            [SettingType(SettingType.Ignore)]
            private static readonly Color BlackRowFret = Color.FromArgb(0xFF, 0x55, 0x55, 0x60); // #555560

            // Star Power keeps each row's own color - a note's black/white identity is the only
            // thing telling the player which button to press, so it must survive Star Power.
            // The Star Power cue is carried by the note casing turning gold instead
            // (Metal vs MetalStarPower below).

            #region Frets

            // One fret per column, split into two lit zones matching the neck: the upper (top)
            // zone is the black row and the lower (inner) zone is the white row (see FretInner
            // below). Both rows of a column share the fret object, so these are all the same
            // value - which of the two note types the shared fret takes its color from is then
            // irrelevant.
            public Color OpenFret   = DefaultPurple;
            public Color Black1Fret = BlackRowFret;
            public Color Black2Fret = BlackRowFret;
            public Color Black3Fret = BlackRowFret;
            public Color White1Fret = BlackRowFret;
            public Color White2Fret = BlackRowFret;
            public Color White3Fret = BlackRowFret;

            /// <summary>
            /// Gets the fret color for a specific note index.
            /// 0 = open note, 1 = black1, 6 = white3.
            /// </summary>
            public Color GetFretColor(int index)
            {
                return index switch
                {
                    (int) SixFretGuitarFret.Black1 => Black1Fret,
                    (int) SixFretGuitarFret.Black2 => Black2Fret,
                    (int) SixFretGuitarFret.Black3 => Black3Fret,
                    (int) SixFretGuitarFret.White1 => White1Fret,
                    (int) SixFretGuitarFret.White2 => White2Fret,
                    (int) SixFretGuitarFret.White3 => White3Fret,
                    (int) SixFretGuitarFret.Open => OpenFret,
                    _ => default
                };
            }

            // The inner (lower) zone of the fret is the white row of that column.
            public Color OpenFretInner   = DefaultPurple;
            public Color Black1FretInner = WhiteRow;
            public Color Black2FretInner = WhiteRow;
            public Color Black3FretInner = WhiteRow;
            public Color White1FretInner = WhiteRow;
            public Color White2FretInner = WhiteRow;
            public Color White3FretInner = WhiteRow;

            /// <summary>
            /// Gets the inner fret color for a specific note index.
            /// 0 = open note, 1 = black1, 6 = white3.
            /// </summary>
            public Color GetFretInnerColor(int index)
            {
                return index switch
                {
                    (int) SixFretGuitarFret.Black1 => Black1FretInner,
                    (int) SixFretGuitarFret.Black2 => Black2FretInner,
                    (int) SixFretGuitarFret.Black3 => Black3FretInner,
                    (int) SixFretGuitarFret.White1 => White1FretInner,
                    (int) SixFretGuitarFret.White2 => White2FretInner,
                    (int) SixFretGuitarFret.White3 => White3FretInner,
                    (int) SixFretGuitarFret.Open => OpenFretInner,
                    _ => default
                };
            }

            // Hit particles follow the row that was hit, so a black hit doesn't spray white.
            public Color OpenParticles   = DefaultPurple;
            public Color Black1Particles = BlackRow;
            public Color Black2Particles = BlackRow;
            public Color Black3Particles = BlackRow;
            public Color White1Particles = WhiteRow;
            public Color White2Particles = WhiteRow;
            public Color White3Particles = WhiteRow;

            /// <summary>
            /// Gets the particle color for a specific note index.
            /// 0 = open note, 1 = black1, 6 = white3.
            /// </summary>
            public Color GetParticleColor(int index)
            {
                return index switch
                {
                    (int) SixFretGuitarFret.Black1 => Black1Particles,
                    (int) SixFretGuitarFret.Black2 => Black2Particles,
                    (int) SixFretGuitarFret.Black3 => Black3Particles,
                    (int) SixFretGuitarFret.White1 => White1Particles,
                    (int) SixFretGuitarFret.White2 => White2Particles,
                    (int) SixFretGuitarFret.White3 => White3Particles,
                    (int) SixFretGuitarFret.Open => OpenParticles,
                    _ => default
                };
            }

            #endregion

            #region Notes

            public Color OpenNote   = DefaultPurple;
            public Color Black1Note = BlackRow;
            public Color Black2Note = BlackRow;
            public Color Black3Note = BlackRow;
            public Color White1Note = WhiteRow;
            public Color White2Note = WhiteRow;
            public Color White3Note = WhiteRow;

            /// <summary>
            /// Gets the note color for a specific note index.
            /// 1 = black1, 6 = white3.
            /// </summary>
            public Color GetNoteColor(int index)
            {
                return index switch
                {
                    (int) SixFretGuitarFret.Black1 => Black1Note,
                    (int) SixFretGuitarFret.Black2 => Black2Note,
                    (int) SixFretGuitarFret.Black3 => Black3Note,
                    (int) SixFretGuitarFret.White1 => White1Note,
                    (int) SixFretGuitarFret.White2 => White2Note,
                    (int) SixFretGuitarFret.White3 => White3Note,
                    (int) SixFretGuitarFret.Wildcard => DefaultWildcard,
                    (int) SixFretGuitarFret.Open => OpenNote,
                    _ => default
                };
            }

            public Color OpenNoteStarPower   = DefaultStarpower;
            public Color Black1NoteStarPower = BlackRow;
            public Color Black2NoteStarPower = BlackRow;
            public Color Black3NoteStarPower = BlackRow;
            public Color White1NoteStarPower = WhiteRow;
            public Color White2NoteStarPower = WhiteRow;
            public Color White3NoteStarPower = WhiteRow;

            /// <summary>
            /// Gets the Star Power note color for a specific note index.
            /// 1 = black1, 6 = white3.
            /// </summary>
            public Color GetNoteStarPowerColor(int index)
            {
                return index switch
                {
                    (int) SixFretGuitarFret.Black1 => Black1NoteStarPower,
                    (int) SixFretGuitarFret.Black2 => Black2NoteStarPower,
                    (int) SixFretGuitarFret.Black3 => Black3NoteStarPower,
                    (int) SixFretGuitarFret.White1 => White1NoteStarPower,
                    (int) SixFretGuitarFret.White2 => White2NoteStarPower,
                    (int) SixFretGuitarFret.White3 => White3NoteStarPower,
                    (int) SixFretGuitarFret.Wildcard => DefaultWildcardStarpower,
                    (int) SixFretGuitarFret.Open => OpenNoteStarPower,
                    _ => default
                };
            }

            #endregion

            /// <summary>
            /// Gets the color of the row a note does NOT belong to.
            /// </summary>
            /// <remarks>
            /// A GHL note is drawn as three alternating tones - a black note is black outside,
            /// white in the middle band, black again in the center - so the middle band needs
            /// the opposite row's color.
            /// </remarks>
            public Color GetOppositeRowNoteColor(int index)
            {
                return index switch
                {
                    (int) SixFretGuitarFret.Black1 or
                    (int) SixFretGuitarFret.Black2 or
                    (int) SixFretGuitarFret.Black3 => WhiteRow,

                    (int) SixFretGuitarFret.White1 or
                    (int) SixFretGuitarFret.White2 or
                    (int) SixFretGuitarFret.White3 => BlackRow,

                    _ => WhiteRow
                };
            }

            #region Metal

            public Color Metal          = DefaultMetal;
            public Color MetalStarPower = DefaultMetalStarPower;

            public Color GetMetalColor(bool isForStarPower)
            {
                return isForStarPower ? MetalStarPower : Metal;
            }

            #endregion

            #region Miss Effect

            public Color Miss = DefaultMiss;

            #endregion

            #region Serialization

            public SixFretGuitarColors Copy()
            {
                // Kinda yucky, but it's easier to maintain
                return (SixFretGuitarColors) MemberwiseClone();
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(OpenFret);
                writer.Write(Black1Fret);
                writer.Write(Black2Fret);
                writer.Write(Black3Fret);
                writer.Write(White1Fret);
                writer.Write(White2Fret);
                writer.Write(White3Fret);

                writer.Write(OpenFretInner);
                writer.Write(Black1FretInner);
                writer.Write(Black2FretInner);
                writer.Write(Black3FretInner);
                writer.Write(White1FretInner);
                writer.Write(White2FretInner);
                writer.Write(White3FretInner);

                writer.Write(OpenParticles);
                writer.Write(Black1Particles);
                writer.Write(Black2Particles);
                writer.Write(Black3Particles);
                writer.Write(White1Particles);
                writer.Write(White2Particles);
                writer.Write(White3Particles);

                writer.Write(OpenNote);
                writer.Write(Black1Note);
                writer.Write(Black2Note);
                writer.Write(Black3Note);
                writer.Write(White1Note);
                writer.Write(White2Note);
                writer.Write(White3Note);

                writer.Write(OpenNoteStarPower);
                writer.Write(Black1NoteStarPower);
                writer.Write(Black2NoteStarPower);
                writer.Write(Black3NoteStarPower);
                writer.Write(White1NoteStarPower);
                writer.Write(White2NoteStarPower);
                writer.Write(White3NoteStarPower);
            }

            public void Deserialize(BinaryReader reader, int version = 0)
            {
                OpenFret = reader.ReadColor();
                Black1Fret = reader.ReadColor();
                Black2Fret = reader.ReadColor();
                Black3Fret = reader.ReadColor();
                White1Fret = reader.ReadColor();
                White2Fret = reader.ReadColor();
                White3Fret = reader.ReadColor();

                OpenFretInner = reader.ReadColor();
                Black1FretInner = reader.ReadColor();
                Black2FretInner = reader.ReadColor();
                Black3FretInner = reader.ReadColor();
                White1FretInner = reader.ReadColor();
                White2FretInner = reader.ReadColor();
                White3FretInner = reader.ReadColor();

                OpenParticles = reader.ReadColor();
                Black1Particles = reader.ReadColor();
                Black2Particles = reader.ReadColor();
                Black3Particles = reader.ReadColor();
                White1Particles = reader.ReadColor();
                White2Particles = reader.ReadColor();
                White3Particles = reader.ReadColor();

                OpenNote = reader.ReadColor();
                Black1Note = reader.ReadColor();
                Black2Note = reader.ReadColor();
                Black3Note = reader.ReadColor();
                White1Note = reader.ReadColor();
                White2Note = reader.ReadColor();
                White3Note = reader.ReadColor();

                OpenNoteStarPower = reader.ReadColor();
                Black1NoteStarPower = reader.ReadColor();
                Black2NoteStarPower = reader.ReadColor();
                Black3NoteStarPower = reader.ReadColor();
                White1NoteStarPower = reader.ReadColor();
                White2NoteStarPower = reader.ReadColor();
                White3NoteStarPower = reader.ReadColor();
            }

            #endregion
        }
    }
}
