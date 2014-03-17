﻿using UnityEngine;
using System.Threading;

/**
 *	A Gaussian Blur effect to be applied to 2D images.
 */
public class GaussianBlur : IImagePostProcessor {

	private float[] coefficients;

	/**
	 *	Applies gaussian blur effect to a bitmap.
	 * 	@param colorBitmap the image to be processed.
	 */
	public void ProcessBitmap(ref ColorBitmap colorBitmap)
	{
		OnionLogger.globalLog.PushInfoLayer("Performing GaussianBlur on bitmap.");

		RGBBitmap rgbBitmap = new RGBBitmap();
		ColorUtils.colorBitmapToRGBBitmap(ref colorBitmap, ref rgbBitmap);

		MonochromeBitmap blurR = ColorUtils.redBitmapFromRGBBitmap(ref rgbBitmap);
		MonochromeBitmap blurG = ColorUtils.greenBitmapFromRGBBitmap(ref rgbBitmap);
		MonochromeBitmap blurB = ColorUtils.blueBitmapFromRGBBitmap(ref rgbBitmap);

		// temp hard-coded
		int numberOfCoefficients = 5;
		this.coefficients = ApproximateGaussianCoefficients(numberOfCoefficients);

		// We can run each channel in parallel.
		OnionLogger.globalLog.PushInfoLayer("Setting up GaussianBlur threads");
		int numberOfThreads = 3;

		// Initialize thread objects
		ManualResetEvent[] threadsDone = new ManualResetEvent[numberOfThreads];
		GaussianBlurThread[] threads = new GaussianBlurThread[numberOfThreads];
		for (int i = 0; i < numberOfThreads; ++i) {
			threadsDone[i] = new ManualResetEvent(false);
			threads[i] = new GaussianBlurThread(threadsDone[i], this.coefficients);
		}
		OnionLogger.globalLog.PopInfoLayer();

		OnionLogger.globalLog.PushInfoLayer("Running Gaussian blur threads");
		ThreadPool.QueueUserWorkItem(new WaitCallback(threads[0].ThreadedProcessChannel), blurR);
		ThreadPool.QueueUserWorkItem(new WaitCallback(threads[1].ThreadedProcessChannel), blurG);
		ThreadPool.QueueUserWorkItem(new WaitCallback(threads[2].ThreadedProcessChannel), blurB);
		
		WaitHandle.WaitAll(threadsDone);
		OnionLogger.globalLog.PopInfoLayer();
		// Done with parallel section.
		
		rgbBitmap.rgb.r = blurR.channel;
		rgbBitmap.rgb.b = blurB.channel;
		rgbBitmap.rgb.g = blurG.channel;

		ColorUtils.RGBBitmapToColorBitmap(ref rgbBitmap, ref colorBitmap);

		OnionLogger.globalLog.PopInfoLayer();
	}
	
	/**
	 * 	Applies gaussian blur effect to a single channel, represented as an array of floats.
	 * 	This is a non-parallel implementation.
	 * 	@param channel A single channel (or monochrome image) to be processed.
	 */
	public void ProcessChannel(ref MonochromeBitmap blurred)
	{
		OnionLogger.globalLog.PushDebugLayer("Blurring channel");
		// We have to make a copy of the bitmap first
		MonochromeBitmap original = ColorUtils.Copy(ref blurred);

		// Then zero the one we are blurring.
		for (int i = 0; i < blurred.channel.Length; ++i) {
			blurred.channel[i] = 0f;
		}

		int numberOfCoefficients = this.coefficients.Length - 1;

		for (int i = 0; i < blurred.height; ++i) {
			for (int j = 0; j < blurred.width; ++j) {
				int targetPixel = i * blurred.width + j;
				OnionLogger.globalLog.PushTraceLayer("Blurring "+targetPixel);

				for (int k = -numberOfCoefficients; k <= numberOfCoefficients; ++k) {
					OnionLogger.globalLog.PushTraceLayer((targetPixel + k).ToString());
					if (j + k < 0 || j + k >= blurred.width) {
						OnionLogger.globalLog.LogTrace((j + k) + " is off of row; skipping");
						OnionLogger.globalLog.PopTraceLayer();
						continue;
					}
					int sourcePoint = targetPixel + k;
					OnionLogger.globalLog.LogTrace("value is "+original.channel[sourcePoint]);
					float factor = coefficients[Mathf.Abs(k)];
					float weightedValue = factor * original.channel[sourcePoint];
					OnionLogger.globalLog.LogTrace("weighted value is "+weightedValue);
					blurred.channel[targetPixel] += weightedValue;
					OnionLogger.globalLog.LogTrace("targetPixel is now "+blurred.channel[targetPixel]);
					OnionLogger.globalLog.PopTraceLayer();
				}

				OnionLogger.globalLog.PopTraceLayer();
			}
		}
		
		OnionLogger.globalLog.PopDebugLayer();
	}

	/**
	 *	Uses an approximation method for calculating a normal distribution
	 *	based on this paper (tweaked a bit so that it is centered at 0 and gives values < 1.
	 *
	 *	“A cosine approximation to the normal distribution” D. H. Raab, E. H. Green,
	 *	Psychometrika, Volume 26, pages 447-450.
	 * 
	 * 	@param count How many Gaussian terms to calculate (in addition to the 0th one, which is always 1.0).
	 */
	public float[] ApproximateGaussianCoefficients(int count)
	{
		OnionLogger.globalLog.PushDebugLayer(
			string.Format("Approximating {0} Gaussian coefficients", count + 1));

		float[] coefficients = new float[count + 1];
		float stepSize = Mathf.PI / coefficients.Length;
		float x = 0;
		for (int index = 0; index < coefficients.Length; ++index) {
			coefficients[index] = (1f + Mathf.Cos(x)) / (2f * Mathf.PI);

			string logStr = string.Format("g({0}) = {1}", x, coefficients[index]);
			OnionLogger.globalLog.LogDebug(logStr);
			x += stepSize;
		}
		OnionLogger.globalLog.PopDebugLayer();
		return coefficients;
	}
}

/**
 *	A thread that blurs a MonochromeBitmap.
 */
public class GaussianBlurThread : IImagePostProcessorThread {
	
	private ManualResetEvent done;
	private readonly float[] coefficients;
	private MonochromeBitmap original;
	
	public GaussianBlurThread(ManualResetEvent handle, 
	                          float[] gaussianCoefficients) 
	{
		this.coefficients = gaussianCoefficients;
		this.done = handle;
	}
	
	/**
	 *	No parallel implementation yet
	 * 	@param state
	 */
	public void ThreadedProcessBitmap(object state)
	{
		// not implemented
	}
	
	/**
	 * 	Blurs a single channel.
	 * 	@param state 	An object containing a MonochromeBitmap.
	 * 					Must be 'object' because of how .NET handles threads.
	 */
	public void ThreadedProcessChannel(object state)
	{
		MonochromeBitmap blurred = (MonochromeBitmap)state;
		original = ColorUtils.Copy(ref blurred);
		BlurHorizontal(ref blurred);
		ColorUtils.Transpose(ref blurred);
		original = ColorUtils.Copy(ref blurred);
		BlurHorizontal(ref blurred);
		ColorUtils.Transpose(ref blurred);

		done.Set();
	}

	private void BlurHorizontal(ref MonochromeBitmap blurred)
	{
		// Zero the bitmap we are blurring.
		for (int i = 0; i < blurred.channel.Length; ++i) {
			blurred.channel[i] = 0f;
		}
		
		int numberOfCoefficients = this.coefficients.Length - 1;
		
		for (int i = 0; i < blurred.height; ++i) {
			for (int j = 0; j < blurred.width; ++j) {
				int targetPixel = i * blurred.width + j;
				
				for (int k = -numberOfCoefficients; k <= numberOfCoefficients; ++k) {
					if (j + k < 0 || j + k >= blurred.width) {
						continue;
					}
					int sourcePoint = targetPixel + k;
					float factor = coefficients[Mathf.Abs(k)];
					float weightedValue = factor * original.channel[sourcePoint];
					blurred.channel[targetPixel] += weightedValue;
				}
			}
		}
	}
}