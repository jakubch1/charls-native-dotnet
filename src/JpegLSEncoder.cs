// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace CharLS.Native
{
    /// <summary>
    /// JPEG-LS Encoder.
    /// </summary>
    public sealed class JpegLSEncoder : IDisposable
    {
        private readonly SafeHandleJpegLSEncoder _encoder = CreateEncoder();
        private FrameInfo? _frameInfo;
        private int _nearLossless;
        private JpegLSInterleaveMode _interleaveMode;
        private MemoryHandle _destinationPin;

        /// <summary>
        /// Gets or sets the frame information.
        /// </summary>
        /// <value>
        /// The frame information.
        /// </value>
        public FrameInfo? FrameInfo
        {
            get => _frameInfo;

            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                FrameInfoNative infoNative = default;

                infoNative.Height = (uint)value.Height;
                infoNative.Width = (uint)value.Width;
                infoNative.BitsPerSample = value.BitsPerSample;
                infoNative.ComponentCount = value.ComponentCount;

                JpegLSError error = SafeNativeMethods.CharLSSetFrameInfo(_encoder, ref infoNative);
                JpegLSCodec.HandleResult(error);

                _frameInfo = value;
            }
        }

        /// <summary>
        /// Gets or sets the near lossless parameter used to encode the JPEG-LS stream.
        /// </summary>
        /// <value>
        /// The near lossless parameter value.
        /// </value>
        public int NearLossless
        {
            get => _nearLossless;

            set
            {
                JpegLSError error = SafeNativeMethods.CharLSSetNearLossless(_encoder, value);
                JpegLSCodec.HandleResult(error);

                _nearLossless = value;
            }
        }

        /// <summary>
        /// Gets or sets the interleave mode.
        /// </summary>
        /// <value>
        /// The interleave mode.
        /// </value>
        public JpegLSInterleaveMode InterleaveMode
        {
            get => _interleaveMode;

            set
            {
                JpegLSCodec.HandleResult(SafeNativeMethods.CharLSSetInterleaveMode(_encoder, value));
                _interleaveMode = value;
            }
        }

        /// <summary>
        /// Gets the size of the estimated destination.
        /// </summary>
        /// <value>
        /// The size of the estimated destination.
        /// </value>
        public long EstimatedDestinationSize
        {
            get
            {
                JpegLSError error = SafeNativeMethods.CharLSGetEstimatedDestinationSize(_encoder, out UIntPtr sizeInBytes);
                JpegLSCodec.HandleResult(error);

                return (long)sizeInBytes;
            }
        }

        /// <summary>
        /// Gets the bytes written.
        /// </summary>
        /// <value>
        /// The bytes written.
        /// </value>
        public long BytesWritten
        {
            get
            {
                JpegLSError error = SafeNativeMethods.CharLSGetBytesWritten(_encoder, out UIntPtr bytesWritten);
                JpegLSCodec.HandleResult(error);

                return (long)bytesWritten;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _encoder.Dispose();
            _destinationPin.Dispose();
        }

        /// <summary>
        /// Sets the destination buffer that contains the pixels that need to be encoded.
        /// </summary>
        /// <param name="destination">The destination buffer.</param>
        public void SetDestination(Memory<byte> destination)
        {
            _destinationPin = destination.Pin();

            try
            {
                unsafe
                {
                    JpegLSError error = SafeNativeMethods.CharLSSetDestinationBuffer(_encoder, (byte*)_destinationPin.Pointer, (UIntPtr)destination.Length);
                    JpegLSCodec.HandleResult(error);
                }
            }
            catch
            {
                _destinationPin.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Encodes the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="stride">The stride.</param>
        public void Encode(ReadOnlySpan<byte> source, int stride = 0)
        {
            JpegLSError error = SafeNativeMethods.CharLSEncodeFromBuffer(_encoder, ref MemoryMarshal.GetReference(source), (UIntPtr)source.Length, (uint)stride);
            JpegLSCodec.HandleResult(error);
        }

        /// <summary>
        /// Writes a standard SPIFF header to the destination. The additional values are computed from the current encoder settings.
        /// A SPIFF header is optional, but recommended for standalone JPEG-LS files.
        /// </summary>
        /// <param name="colorSpace">The color space of the image.</param>
        /// <param name="resolutionUnit">The resolution units of the next 2 parameters.</param>
        /// <param name="verticalResolution">The vertical resolution.</param>
        /// <param name="horizontalResolution">The horizontal resolution.</param>
        public void WriteStandardSpiffHeader(SpiffColorSpace colorSpace, SpiffResolutionUnit resolutionUnit = SpiffResolutionUnit.AspectRatio,
            int verticalResolution = 1, int horizontalResolution = 1)
        {
            JpegLSError error = SafeNativeMethods.CharLSWriteStandardSpiffHeader(_encoder, colorSpace, resolutionUnit, (uint)verticalResolution, (uint)horizontalResolution);
            JpegLSCodec.HandleResult(error);
        }

        /// <summary>
        /// Writes a SPIFF header to the destination.
        /// </summary>
        /// <param name="spiffHeader">Reference to a SPIFF header that will be written to the destination.</param>
        public void WriteSpiffHeader(SpiffHeader spiffHeader)
        {
            var headerNative = new SpiffHeaderNative(spiffHeader);

            JpegLSError error = SafeNativeMethods.CharLSWriteSpiffHeader(_encoder, ref headerNative);
            JpegLSCodec.HandleResult(error);
        }

        private static SafeHandleJpegLSEncoder CreateEncoder()
        {
            SafeHandleJpegLSEncoder encoder = SafeNativeMethods.CharLSCreateEncoder();
            return encoder.IsInvalid ? throw new OutOfMemoryException() : encoder;
        }
    }
}
