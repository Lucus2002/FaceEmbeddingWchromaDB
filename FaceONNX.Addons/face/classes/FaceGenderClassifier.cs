﻿using FaceONNX.Addons.Properties;
using FaceONNX.Models.face.interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UMapx.Core;
using UMapx.Imaging;

namespace FaceONNX
{
	/// <summary>
	/// Defines face gender classifier.
	/// </summary>
	public class FaceGenderClassifier : IFaceClassifier
	{
		#region Private data
		/// <summary>
		/// Inference session.
		/// </summary>
		private readonly InferenceSession _session;
		#endregion

		#region Constructor

		/// <summary>
		/// Initializes face gender classifier.
		/// </summary>
		public FaceGenderClassifier()
		{
			_session = new InferenceSession(Resources.gender_efficientnet_b2);
		}

		/// <summary>
		/// Initializes face gender classifier.
		/// </summary>
		/// <param name="options">Session options</param>
		public FaceGenderClassifier(SessionOptions options)
		{
			_session = new InferenceSession(Resources.gender_efficientnet_b2, options);
		}

        #endregion

        #region Properties

        /// <summary>
        /// Returns the labels.
        /// </summary>
        public static readonly string[] Labels = new string[] { "Male", "Female" };

		#endregion

		#region Methods

		/// <inheritdoc/>
		public float[] Forward(Bitmap image)
		{
            var rgb = image.ToRGB(false);
            return Forward(rgb);
        }

        /// <inheritdoc/>
        public float[] Forward(float[][,] image)
        {
            if (image.Length != 3)
                throw new ArgumentException("Image must be in BGR terms");

            var size = new Size(224, 224);
            var resized = new float[3][,];

            for (int i = 0; i < image.Length; i++)
            {
                resized[i] = image[i].Resize(size.Height, size.Width, InterpolationMode.Bilinear);
            }

            var inputMeta = _session.InputMetadata;
            var name = inputMeta.Keys.ToArray()[0];

            // pre-processing
            var dimentions = new int[] { 1, 3, size.Height, size.Width };
            var tensors = resized.ToFloatTensor(true);
            tensors.Compute(255.0f, Matrice.Div);                           // scale
            tensors.Compute(new[] { 0.485f, 0.456f, 0.406f }, Matrice.Sub); // mean
            tensors.Compute(new[] { 0.229f, 0.224f, 0.225f }, Matrice.Div); // std
            var inputData = tensors.Merge(true);

            // session run
            var t = new DenseTensor<float>(inputData, dimentions);
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(name, t) };
            using var outputs = _session.Run(inputs);
            var results = outputs.ToArray();
            var length = results.Length;
            var confidences = results[length - 1].AsTensor<float>().ToArray();

            // dispose
            foreach (var result in results)
            {
                result.Dispose();
            }

            return confidences;
        }

        #endregion

        #region IDisposable

        private bool _disposed;

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_session?.Dispose();
				}

				_disposed = true;
			}
		}

        /// <summary>
        /// Destructor.
        /// </summary>
        ~FaceGenderClassifier()
		{
			Dispose(false);
		}

		#endregion
	}
}
