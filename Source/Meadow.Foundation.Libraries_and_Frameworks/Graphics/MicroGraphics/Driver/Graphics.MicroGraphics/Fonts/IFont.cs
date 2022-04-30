<<<<<<<< HEAD:Source/Meadow.Foundation.Libraries_and_Frameworks/Graphics/MicroGraphics/Driver/Graphics.MicroGraphics/Fonts/IFont.cs
﻿namespace Meadow.Foundation.Graphics
{
    public interface IFont
========
﻿namespace Meadow.Foundation.Font {
    /// <summary>
    ///     Abstract class for a Font.
    /// </summary>
    public abstract class FontBase
>>>>>>>> 44bdd4892d6fd76fd0305b6ae4f8baaac46e4030:Source/Meadow.Foundation.Core/Font/FontBase.cs
    {
        /// <summary>
        ///     Width of a character in the font.
        /// </summary>
        public abstract int Width { get; }

        /// <summary>
        ///     Height of a character in the font.
        /// </summary>
        public abstract int Height { get; }

        /// <summary>
        ///     Get the binary representation of the ASCII character from the font table.
        /// </summary>
        /// <param name="character">Character to look up.</param>
        /// <returns>Array of bytes representing the binary bit pattern of the character.</returns>
        public abstract byte[] this[char character] { get; }
    }
}
