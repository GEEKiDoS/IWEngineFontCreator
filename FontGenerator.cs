using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IWEngine;
using StbTrueTypeSharp;

using PackedChar = StbTrueTypeSharp.StbTrueType.stbtt_packedchar;
using PackRange = StbTrueTypeSharp.StbTrueType.stbtt_pack_range;
using PackContext = StbTrueTypeSharp.StbTrueType.stbtt_pack_context;
using System.Windows;

namespace IWEngineFontCreator
{
    public class CharacterRange
    {
        public static readonly CharacterRange BasicLatin = new CharacterRange(0x0020, 0x007F);
        public static readonly CharacterRange Latin1Supplement = new CharacterRange(0x00A0, 0x00FF);
        public static readonly CharacterRange LatinExtendedA = new CharacterRange(0x0100, 0x017F);
        public static readonly CharacterRange LatinExtendedB = new CharacterRange(0x0180, 0x024F);
        public static readonly CharacterRange Cyrillic = new CharacterRange(0x0400, 0x04FF);
        public static readonly CharacterRange CyrillicSupplement = new CharacterRange(0x0500, 0x052F);
        public static readonly CharacterRange Hiragana = new CharacterRange(0x3040, 0x309F);
        public static readonly CharacterRange Katakana = new CharacterRange(0x30A0, 0x30FF);
        public static readonly CharacterRange Greek = new CharacterRange(0x0370, 0x03FF);
        public static readonly CharacterRange CjkSymbolsAndPunctuation = new CharacterRange(0x3000, 0x303F);
        public static readonly CharacterRange CjkUnifiedIdeographs = new CharacterRange(0x4e00, 0x9fff);
        public static readonly CharacterRange HangulCompatibilityJamo = new CharacterRange(0x3130, 0x318f);
        public static readonly CharacterRange HangulSyllables = new CharacterRange(0xac00, 0xd7af);
        public static readonly CharacterRange FullWidth = new CharacterRange(0xff00, 0xffef);

        public int Start
        {
            get;
        }

        public int End
        {
            get;
        }

        public int Size
        {
            get
            {
                return End - Start + 1;
            }
        }

        public CharacterRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public CharacterRange(int single) : this(single, single)
        {
        }

        public override string ToString()
        {
            return $"(Unicode) {Start:x} - {End:x}";
        }
    }

    public unsafe class FontBaker
    {
        private byte[] _bitmap;
        private PackContext _context;
        private Font _font;
        private int bitmapWidth, bitmapHeight;
        private int _yOffset;

        public void Begin(int width, int height, int yOffset = 0, byte overSamplintX = 1, byte overSamplintY = 1)
        {
            bitmapWidth = width;
            bitmapHeight = height;
            _bitmap = new byte[width * height];
            _context = new PackContext();

            fixed (byte* pixelsPtr = _bitmap)
            {
                StbTrueType.stbtt_PackBegin(_context, pixelsPtr, width, height, width, 1, null);
            }

            _context.skip_missing = 0;

            StbTrueType.stbtt_PackSetOversampling(_context, overSamplintX, overSamplintY);

            _font = new Font
            {
                Glyphs = new()
            };
            _yOffset = yOffset;
        }

        public void Add(byte[] ttf, float fontPixelHeight, IEnumerable<CharacterRange> characterRanges)
        {
            if (ttf == null || ttf.Length == 0)
                throw new ArgumentNullException(nameof(ttf));

            if (fontPixelHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(fontPixelHeight));

            if (characterRanges == null)
                throw new ArgumentNullException(nameof(characterRanges));

            if (!characterRanges.Any())
                throw new ArgumentException("characterRanges must have a least one value.");

            var fontInfo = StbTrueType.CreateFont(ttf, 0);
            if (fontInfo == null)
                throw new Exception("Failed to init font.");

            _font.PixelHeight = (int)fontPixelHeight;

            var scaleFactor = StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, fontPixelHeight);

            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(fontInfo, &ascent, &descent, &lineGap);

            foreach (var range in characterRanges)
            {
                if (range.Start > range.End)
                    continue;

                var cd = new PackedChar[range.End - range.Start + 1];
                fixed (PackedChar* chardataPtr = cd)
                {
                    int sizeRemaining = StbTrueType.stbtt_PackFontRange(_context, fontInfo.data, 0, fontPixelHeight,
                        range.Start,
                        range.End - range.Start + 1,
                        chardataPtr);
                }

                for (var i = 0; i < cd.Length; ++i)
                {
                    var pch = cd[i];

                    var yOff = cd[i].yoff;
                    yOff += ascent * scaleFactor;

                    _font.Glyphs.Add(new Glyph
                    {
                        Letter = (ushort)(i + range.Start),
                        X0 = (sbyte)Math.Round(pch.xoff),
                        Y0 = (sbyte)Math.Round(yOff + _yOffset),
                        S0 = pch.x0 / (float)bitmapWidth,
                        S1 = pch.x1 / (float)bitmapWidth,
                        T0 = pch.y0 / (float)bitmapHeight,
                        T1 = pch.y1 / (float)bitmapHeight,
                        PixelWidth = (sbyte)(pch.xoff2 - pch.xoff),
                        PixelHeight = (sbyte)(pch.yoff2 - pch.yoff),
                        Dx = (sbyte)Math.Round(pch.xadvance)
                    });
                }
            }
        }

        public (byte[], Font) End(string name, Encoding encoding = null)
        {
            // encoding ??= Encoding.ASCII;

            foreach(var g in _font.Glyphs)
            {
                int newLetter = g.Letter;

                var bytes = encoding.GetBytes(((char)g.Letter).ToString());
                if (bytes.Length == 2)
                    newLetter = BitConverter.ToUInt16(bytes);
                else if (bytes.Length > 2)
                    newLetter = 0;

                g.Letter = (ushort)newLetter;
            }

            _font.Glyphs = _font.Glyphs.Where(g => g.Letter != 0).ToList();
            _font.Glyphs.Sort();

            _font.GlyphCount = _font.Glyphs.Count;
            _font.Material = $"fonts/{name}";
            _font.FontName = $"fonts/{name}";
            _font.GlowMaterial = $"fonts/{name}_glow";

            return (_bitmap, _font);
        }
    }
}
