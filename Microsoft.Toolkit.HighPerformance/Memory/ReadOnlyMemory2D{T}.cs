﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Microsoft.Toolkit.HighPerformance.Memory.Internals;
using Microsoft.Toolkit.HighPerformance.Memory.Views;

namespace Microsoft.Toolkit.HighPerformance.Memory
{
    /// <summary>
    /// A readonly version of <see cref="Memory2D{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the current <see cref="ReadOnlyMemory2D{T}"/> instance.</typeparam>
    [DebuggerTypeProxy(typeof(MemoryDebugView2D<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    public readonly struct ReadOnlyMemory2D<T> : IEquatable<ReadOnlyMemory2D<T>>
    {
        /// <summary>
        /// The target <see cref="object"/> instance, if present.
        /// </summary>
        private readonly object? instance;

        /// <summary>
        /// The initial offset within <see cref="instance"/>.
        /// </summary>
        private readonly IntPtr offset;

        /// <summary>
        /// The height of the specified 2D region.
        /// </summary>
        private readonly int height;

        /// <summary>
        /// The width of the specified 2D region.
        /// </summary>
        private readonly int width;

        /// <summary>
        /// The pitch of the specified 2D region.
        /// </summary>
        private readonly int pitch;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct.
        /// </summary>
        /// <param name="text">The target <see cref="string"/> to wrap.</param>
        /// <param name="height">The height of the resulting 2D area.</param>
        /// <param name="width">The width of each row in the resulting 2D area.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when either <paramref name="height"/> or <paramref name="width"/> are invalid.
        /// </exception>
        /// <remarks>The total area must match the lenght of <paramref name="text"/>.</remarks>
        public ReadOnlyMemory2D(string text, int height, int width)
            : this(text, 0, height, width, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct.
        /// </summary>
        /// <param name="text">The target <see cref="string"/> to wrap.</param>
        /// <param name="offset">The initial offset within <paramref name="text"/>.</param>
        /// <param name="height">The height of the resulting 2D area.</param>
        /// <param name="width">The width of each row in the resulting 2D area.</param>
        /// <param name="pitch">The pitch in the resulting 2D area.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when one of the input parameters is out of range.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the requested area is outside of bounds for <paramref name="text"/>.
        /// </exception>
        public ReadOnlyMemory2D(string text, int offset, int height, int width, int pitch)
        {
            if ((uint)offset > (uint)text.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForOffset();
            }

            if (height < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
            }

            if (width < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
            }

            if (pitch < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForPitch();
            }

            if (width == 0 || height == 0)
            {
                this = default;

                return;
            }

            int
                remaining = text.Length - offset,
                area = ((width + pitch) * (height - 1)) + width;

            if (area > remaining)
            {
                ThrowHelper.ThrowArgumentException();
            }

            this.instance = text;
            this.offset = text.DangerousGetObjectDataByteOffset(ref text.DangerousGetReferenceAt(offset));
            this.height = height;
            this.width = width;
            this.pitch = pitch;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct.
        /// </summary>
        /// <param name="array">The target array to wrap.</param>
        /// <param name="height">The height of the resulting 2D area.</param>
        /// <param name="width">The width of each row in the resulting 2D area.</param>
        /// <exception cref="ArrayTypeMismatchException">
        /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when either <paramref name="height"/> or <paramref name="width"/> are invalid.
        /// </exception>
        /// <remarks>The total area must match the lenght of <paramref name="array"/>.</remarks>
        public ReadOnlyMemory2D(T[] array, int height, int width)
            : this(array, 0, height, width, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct.
        /// </summary>
        /// <param name="array">The target array to wrap.</param>
        /// <param name="offset">The initial offset within <paramref name="array"/>.</param>
        /// <param name="height">The height of the resulting 2D area.</param>
        /// <param name="width">The width of each row in the resulting 2D area.</param>
        /// <param name="pitch">The pitch in the resulting 2D area.</param>
        /// <exception cref="ArrayTypeMismatchException">
        /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when one of the input parameters is out of range.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the requested area is outside of bounds for <paramref name="array"/>.
        /// </exception>
        public ReadOnlyMemory2D(T[] array, int offset, int height, int width, int pitch)
        {
            if (array.IsCovariant())
            {
                ThrowHelper.ThrowArrayTypeMismatchException();
            }

            if ((uint)offset > (uint)array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForOffset();
            }

            if (height < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
            }

            if (width < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
            }

            if (pitch < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForPitch();
            }

            if (width == 0 || height == 0)
            {
                this = default;

                return;
            }

            int
                remaining = array.Length - offset,
                area = ((width + pitch) * (height - 1)) + width;

            if (area > remaining)
            {
                ThrowHelper.ThrowArgumentException();
            }

            this.instance = array;
            this.offset = array.DangerousGetObjectDataByteOffset(ref array.DangerousGetReferenceAt(offset));
            this.height = height;
            this.width = width;
            this.pitch = pitch;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct wrapping a 2D array.
        /// </summary>
        /// <param name="array">The given 2D array to wrap.</param>
        /// <exception cref="ArrayTypeMismatchException">
        /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
        /// </exception>
        public ReadOnlyMemory2D(T[,]? array)
        {
            if (array is null)
            {
                this = default;

                return;
            }

            if (array.IsCovariant())
            {
                ThrowHelper.ThrowArrayTypeMismatchException();
            }

            this.instance = array;
            this.offset = array.DangerousGetObjectDataByteOffset(ref array.DangerousGetReferenceAt(0, 0));
            this.height = array.GetLength(0);
            this.width = array.GetLength(1);
            this.pitch = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct wrapping a 2D array.
        /// </summary>
        /// <param name="array">The given 2D array to wrap.</param>
        /// <param name="row">The target row to map within <paramref name="array"/>.</param>
        /// <param name="column">The target column to map within <paramref name="array"/>.</param>
        /// <param name="height">The height to map within <paramref name="array"/>.</param>
        /// <param name="width">The width to map within <paramref name="array"/>.</param>
        /// <exception cref="ArrayTypeMismatchException">
        /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when either <paramref name="height"/>, <paramref name="width"/> or <paramref name="height"/>
        /// are negative or not within the bounds that are valid for <paramref name="array"/>.
        /// </exception>
        public ReadOnlyMemory2D(T[,] array, int row, int column, int height, int width)
        {
            if (array.IsCovariant())
            {
                ThrowHelper.ThrowArrayTypeMismatchException();
            }

            int
                rows = array.GetLength(0),
                columns = array.GetLength(1);

            if ((uint)row >= (uint)rows)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForRow();
            }

            if ((uint)column >= (uint)columns)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForColumn();
            }

            if ((uint)height > (uint)(rows - row))
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
            }

            if ((uint)width > (uint)(columns - column))
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
            }

            this.instance = array;
            this.offset = array.DangerousGetObjectDataByteOffset(ref array.DangerousGetReferenceAt(row, column));
            this.height = height;
            this.width = width;
            this.pitch = columns - width;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct wrapping a layer in a 3D array.
        /// </summary>
        /// <param name="array">The given 3D array to wrap.</param>
        /// <param name="depth">The target layer to map within <paramref name="array"/>.</param>
        /// <exception cref="ArrayTypeMismatchException">
        /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a parameter is invalid.</exception>
        public ReadOnlyMemory2D(T[,,] array, int depth)
        {
            if (array.IsCovariant())
            {
                ThrowHelper.ThrowArrayTypeMismatchException();
            }

            if ((uint)depth >= (uint)array.GetLength(0))
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForDepth();
            }

            this.instance = array;
            this.offset = array.DangerousGetObjectDataByteOffset(ref array.DangerousGetReferenceAt(depth, 0, 0));
            this.height = array.GetLength(1);
            this.width = array.GetLength(2);
            this.pitch = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct wrapping a layer in a 3D array.
        /// </summary>
        /// <param name="array">The given 3D array to wrap.</param>
        /// <param name="depth">The target layer to map within <paramref name="array"/>.</param>
        /// <param name="row">The target row to map within <paramref name="array"/>.</param>
        /// <param name="column">The target column to map within <paramref name="array"/>.</param>
        /// <param name="height">The height to map within <paramref name="array"/>.</param>
        /// <param name="width">The width to map within <paramref name="array"/>.</param>
        /// <exception cref="ArrayTypeMismatchException">
        /// Thrown when <paramref name="array"/> doesn't match <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a parameter is invalid.</exception>
        public ReadOnlyMemory2D(T[,,] array, int depth, int row, int column, int height, int width)
        {
            if (array.IsCovariant())
            {
                ThrowHelper.ThrowArrayTypeMismatchException();
            }

            if ((uint)depth >= (uint)array.GetLength(0))
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForDepth();
            }

            int
                rows = array.GetLength(1),
                columns = array.GetLength(2);

            if ((uint)row >= (uint)rows)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForRow();
            }

            if ((uint)column >= (uint)columns)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForColumn();
            }

            if ((uint)height > (uint)(rows - row))
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
            }

            if ((uint)width > (uint)(columns - column))
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
            }

            this.instance = array;
            this.offset = array.DangerousGetObjectDataByteOffset(ref array.DangerousGetReferenceAt(depth, row, column));
            this.height = height;
            this.width = width;
            this.pitch = columns - width;
        }

#if SPAN_RUNTIME_SUPPORT
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct.
        /// </summary>
        /// <param name="memory">The target <see cref="ReadOnlyMemory{T}"/> to wrap.</param>
        /// <param name="height">The height of the resulting 2D area.</param>
        /// <param name="width">The width of each row in the resulting 2D area.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when either <paramref name="height"/> or <paramref name="width"/> are invalid.
        /// </exception>
        /// <remarks>The total area must match the lenght of <paramref name="memory"/>.</remarks>
        public ReadOnlyMemory2D(ReadOnlyMemory<T> memory, int height, int width)
            : this(memory, 0, height, width, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct.
        /// </summary>
        /// <param name="memory">The target <see cref="ReadOnlyMemory{T}"/> to wrap.</param>
        /// <param name="offset">The initial offset within <paramref name="memory"/>.</param>
        /// <param name="height">The height of the resulting 2D area.</param>
        /// <param name="width">The width of each row in the resulting 2D area.</param>
        /// <param name="pitch">The pitch in the resulting 2D area.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when one of the input parameters is out of range.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the requested area is outside of bounds for <paramref name="memory"/>.
        /// </exception>
        public ReadOnlyMemory2D(ReadOnlyMemory<T> memory, int offset, int height, int width, int pitch)
        {
            if ((uint)offset > (uint)memory.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForOffset();
            }

            if (height < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
            }

            if (width < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
            }

            if (pitch < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForPitch();
            }

            if (width == 0 || height == 0)
            {
                this = default;

                return;
            }

            int
                remaining = memory.Length - offset,
                area = ((width + pitch) * (height - 1)) + width;

            if (area > remaining)
            {
                ThrowHelper.ThrowArgumentException();
            }

            // Check whether the memory wraps a string we can directly access
            if (typeof(T) == typeof(char) &&
                MemoryMarshal.TryGetString(Unsafe.As<ReadOnlyMemory<T>, ReadOnlyMemory<char>>(ref memory), out string? text, out int start, out _))
            {
                ref char r0 = ref text.DangerousGetReferenceAt(start + offset);

                this.instance = text;
                this.offset = text.DangerousGetObjectDataByteOffset(ref r0);
            }
            else if (MemoryMarshal.TryGetArray(memory, out ArraySegment<T> segment))
            {
                // Access the array directly, if possible, just like in Memory2D<T>
                T[] array = segment.Array!;

                this.instance = array;
                this.offset = array.DangerousGetObjectDataByteOffset(ref array.DangerousGetReferenceAt(offset)) + segment.Offset;
            }
            else
            {
                this.instance = memory.Slice(offset);
                this.offset = default;
            }

            this.height = height;
            this.width = width;
            this.pitch = pitch;
        }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemory2D{T}"/> struct with the specified parameters.
        /// </summary>
        /// <param name="instance">The target <see cref="object"/> instance.</param>
        /// <param name="offset">The initial offset within <see cref="instance"/>.</param>
        /// <param name="height">The height of the 2D memory area to map.</param>
        /// <param name="width">The width of the 2D memory area to map.</param>
        /// <param name="pitch">The pitch of the 2D memory area to map.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlyMemory2D(object instance, IntPtr offset, int height, int width, int pitch)
        {
            this.instance = instance;
            this.offset = offset;
            this.height = height;
            this.width = width;
            this.pitch = pitch;
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyMemory2D{T}"/> instance from an arbitrary object.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> instance holding the data to map.</param>
        /// <param name="value">The target reference to point to (it must be within <paramref name="instance"/>).</param>
        /// <param name="height">The height of the 2D memory area to map.</param>
        /// <param name="width">The width of the 2D memory area to map.</param>
        /// <param name="pitch">The pitch of the 2D memory area to map.</param>
        /// <returns>A <see cref="ReadOnlyMemory2D{T}"/> instance with the specified parameters.</returns>
        /// <remarks>The <paramref name="value"/> parameter is not validated, and it's responsability of the caller to ensure it's valid.</remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="instance"/> is of an unsupported type.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when one of the input parameters is out of range.
        /// </exception>
        [Pure]
        public static ReadOnlyMemory2D<T> DangerousCreate(object instance, ref T value, int height, int width, int pitch)
        {
            if (instance.GetType() == typeof(Memory<T>))
            {
                ThrowHelper.ThrowArgumentExceptionForUnsupportedType();
            }

            if (instance.GetType() == typeof(ReadOnlyMemory<T>))
            {
                ThrowHelper.ThrowArgumentExceptionForUnsupportedType();
            }

            if (height < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
            }

            if (width < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
            }

            if (pitch < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForPitch();
            }

            IntPtr offset = instance.DangerousGetObjectDataByteOffset(ref value);

            return new ReadOnlyMemory2D<T>(instance, offset, height, width, pitch);
        }

        /// <summary>
        /// Gets an empty <see cref="ReadOnlyMemory2D{T}"/> instance.
        /// </summary>
        public static ReadOnlyMemory2D<T> Empty => default;

        /// <summary>
        /// Gets a value indicating whether the current <see cref="ReadOnlyMemory2D{T}"/> instance is empty.
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (this.height | this.width) == 0;
        }

        /// <summary>
        /// Gets the length of the current <see cref="ReadOnlyMemory2D{T}"/> instance.
        /// </summary>
        public int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Height * Width;
        }

        /// <summary>
        /// Gets the height of the underlying 2D memory area.
        /// </summary>
        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.height;
        }

        /// <summary>
        /// Gets the width of the underlying 2D memory area.
        /// </summary>
        public int Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.width;
        }

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan2D{T}"/> instance from the current memory.
        /// </summary>
        public ReadOnlySpan2D<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!(this.instance is null))
                {
#if SPAN_RUNTIME_SUPPORT
                    if (this.instance.GetType() == typeof(ReadOnlyMemory<T>))
                    {
                        ReadOnlyMemory<T> memory = (ReadOnlyMemory<T>)this.instance;

                        // If the wrapped object is a ReadOnlyMemory<T>, it is always pre-offset
                        ref T r0 = ref memory.Span.DangerousGetReference();

                        return new ReadOnlySpan2D<T>(r0, this.height, this.width, this.pitch);
                    }
                    else
                    {
                        // This handles both arrays and strings
                        ref T r0 = ref this.instance.DangerousGetObjectDataReferenceAt<T>(this.offset);

                        return new ReadOnlySpan2D<T>(r0, this.height, this.width, this.pitch);
                    }
#else
                    return new ReadOnlySpan2D<T>(this.instance, this.offset, this.height, this.width, this.pitch);
#endif
                }

                return default;
            }
        }

        /// <summary>
        /// Slices the current instance with the specified parameters.
        /// </summary>
        /// <param name="row">The target row to map within the current instance.</param>
        /// <param name="column">The target column to map within the current instance.</param>
        /// <param name="height">The height to map within the current instance.</param>
        /// <param name="width">The width to map within the current instance.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when either <paramref name="height"/>, <paramref name="width"/> or <paramref name="height"/>
        /// are negative or not within the bounds that are valid for the current instance.
        /// </exception>
        /// <returns>A new <see cref="ReadOnlyMemory2D{T}"/> instance representing a slice of the current one.</returns>
        [Pure]
        public ReadOnlyMemory2D<T> Slice(int row, int column, int height, int width)
        {
            if ((uint)row >= Height)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForRow();
            }

            if ((uint)column >= this.width)
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForColumn();
            }

            if ((uint)height > (Height - row))
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForHeight();
            }

            if ((uint)width > (this.width - column))
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionForWidth();
            }

            int
                shift = ((this.width + this.pitch) * row) + column,
                pitch = this.pitch + (this.width - width);

            IntPtr offset = this.offset + (shift * Unsafe.SizeOf<T>());

            if (this.instance!.GetType() == typeof(ReadOnlyMemory<T>))
            {
                object instance = ((ReadOnlyMemory<T>)this.instance).Slice((int)offset);

                return new ReadOnlyMemory2D<T>(instance, default, height, width, pitch);
            }

            return new ReadOnlyMemory2D<T>(this.instance!, offset, height, width, pitch);
        }

        /// <summary>
        /// Copies the contents of this <see cref="ReadOnlyMemory2D{T}"/> into a destination <see cref="Memory{T}"/> instance.
        /// </summary>
        /// <param name="destination">The destination <see cref="Memory{T}"/> instance.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination" /> is shorter than the source <see cref="ReadOnlyMemory2D{T}"/> instance.
        /// </exception>
        public void CopyTo(Memory<T> destination) => Span.CopyTo(destination.Span);

        /// <summary>
        /// Attempts to copy the current <see cref="ReadOnlyMemory2D{T}"/> instance to a destination <see cref="Memory{T}"/>.
        /// </summary>
        /// <param name="destination">The target <see cref="Memory{T}"/> of the copy operation.</param>
        /// <returns>Whether or not the operation was successful.</returns>
        public bool TryCopyTo(Memory<T> destination) => Span.TryCopyTo(destination.Span);

        /// <summary>
        /// Copies the contents of this <see cref="ReadOnlyMemory2D{T}"/> into a destination <see cref="Memory2D{T}"/> instance.
        /// For this API to succeed, the target <see cref="Memory2D{T}"/> has to have the same shape as the current one.
        /// </summary>
        /// <param name="destination">The destination <see cref="Memory2D{T}"/> instance.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination" /> is shorter than the source <see cref="ReadOnlyMemory2D{T}"/> instance.
        /// </exception>
        public void CopyTo(Memory2D<T> destination) => Span.CopyTo(destination.Span);

        /// <summary>
        /// Attempts to copy the current <see cref="ReadOnlyMemory2D{T}"/> instance to a destination <see cref="Memory2D{T}"/>.
        /// For this API to succeed, the target <see cref="Memory2D{T}"/> has to have the same shape as the current one.
        /// </summary>
        /// <param name="destination">The target <see cref="Memory2D{T}"/> of the copy operation.</param>
        /// <returns>Whether or not the operation was successful.</returns>
        public bool TryCopyTo(Memory2D<T> destination) => Span.TryCopyTo(destination.Span);

        /// <summary>
        /// Creates a handle for the memory.
        /// The GC will not move the memory until the returned <see cref="MemoryHandle"/>
        /// is disposed, enabling taking and using the memory's address.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// An instance with nonprimitive (non-blittable) members cannot be pinned.
        /// </exception>
        /// <returns>A <see cref="MemoryHandle"/> instance wrapping the pinned handle.</returns>
        public unsafe MemoryHandle Pin()
        {
            if (!(this.instance is null))
            {
                if (this.instance.GetType() == typeof(ReadOnlyMemory<T>))
                {
                    return ((ReadOnlyMemory<T>)this.instance).Pin();
                }

                GCHandle handle = GCHandle.Alloc(this.instance, GCHandleType.Pinned);

                void* pointer = Unsafe.AsPointer(ref this.instance.DangerousGetObjectDataReferenceAt<T>(this.offset));

                return new MemoryHandle(pointer, handle);
            }

            return default;
        }

        /// <summary>
        /// Tries to get a <see cref="ReadOnlyMemory{T}"/> instance, if the underlying buffer is contiguous.
        /// </summary>
        /// <param name="memory">The resulting <see cref="ReadOnlyMemory{T}"/>, in case of success.</param>
        /// <returns>Whether or not <paramref name="memory"/> was correctly assigned.</returns>
        public bool TryGetMemory(out ReadOnlyMemory<T> memory)
        {
            if (this.pitch == 0)
            {
                // Empty Memory2D<T> instance
                if (this.instance is null)
                {
                    memory = default;
                }
                else if (typeof(T) == typeof(char) && this.instance.GetType() == typeof(string))
                {
                    string text = Unsafe.As<string>(this.instance);
                    int index = text.AsSpan().IndexOf(in text.DangerousGetObjectDataReferenceAt<char>(this.offset));
                    ReadOnlyMemory<char> temp = text.AsMemory(index, Size);

                    memory = Unsafe.As<ReadOnlyMemory<char>, ReadOnlyMemory<T>>(ref temp);
                }
                else if (this.instance.GetType() == typeof(ReadOnlyMemory<T>))
                {
                    // If the object is a ReadOnlyMemory<T>, just slice it as needed
                    memory = ((ReadOnlyMemory<T>)this.instance).Slice(0, this.height * this.width);
                }
                else if (this.instance.GetType() == typeof(T[]))
                {
                    // If it's a T[] array, also handle the initial offset
                    T[] array = Unsafe.As<T[]>(this.instance);
                    int index = array.AsSpan().IndexOf(ref array.DangerousGetObjectDataReferenceAt<T>(this.offset));

                    memory = array.AsMemory(index, this.height * this.width);
                }
                else
                {
                    // Reuse a single failure path to reduce
                    // the number of returns in the method
                    goto Failure;
                }

                return true;
            }

            Failure:

            memory = default;

            return false;
        }

        /// <summary>
        /// Copies the contents of the current <see cref="ReadOnlyMemory2D{T}"/> instance into a new 2D array.
        /// </summary>
        /// <returns>A 2D array containing the data in the current <see cref="ReadOnlyMemory2D{T}"/> instance.</returns>
        [Pure]
        public T[,] ToArray() => Span.ToArray();

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj)
        {
            if (obj is ReadOnlyMemory2D<T> readOnlyMemory)
            {
                return Equals(readOnlyMemory);
            }

            if (obj is Memory2D<T> memory)
            {
                return Equals(memory);
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ReadOnlyMemory2D<T> other)
        {
            return
                this.instance == other.instance &&
                this.offset == other.offset &&
                this.height == other.height &&
                this.width == other.width &&
                this.pitch == other.pitch;
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            if (!(this.instance is null))
            {
#if !NETSTANDARD1_4
                return HashCode.Combine(
                    RuntimeHelpers.GetHashCode(this.instance),
                    this.offset,
                    this.height,
                    this.width,
                    this.pitch);
#else
                Span<int> values = stackalloc int[]
                {
                    RuntimeHelpers.GetHashCode(this.instance),
                    this.offset.GetHashCode(),
                    this.height,
                    this.width,
                    this.pitch
                };

                return values.GetDjb2HashCode();
#endif
            }

            return 0;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Microsoft.Toolkit.HighPerformance.Memory.ReadOnlyMemory2D<{typeof(T)}>[{this.height}, {this.width}]";
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="ReadOnlyMemory2D{T}"/>
        /// </summary>
        public static implicit operator ReadOnlyMemory2D<T>(T[,]? array) => new ReadOnlyMemory2D<T>(array);

        /// <summary>
        /// Defines an implicit conversion of a <see cref="Memory2D{T}"/> to a <see cref="ReadOnlyMemory2D{T}"/>
        /// </summary>
        public static implicit operator ReadOnlyMemory2D<T>(Memory2D<T> memory) => Unsafe.As<Memory2D<T>, ReadOnlyMemory2D<T>>(ref memory);
    }
}
