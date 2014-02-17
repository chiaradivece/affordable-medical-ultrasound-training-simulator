using UnityEngine;
using System.Collections;

/** 
 *  Static class to house some useful miscellaneous debug functions.
 */
public static class UltrasoundDebug {

	/**
	 * 	Make an assertion.
	 *  If the assertion is true, continue execution normally.
	 * 	An error is logged if the assertion is false.
	 * 	@param assertion the assertion to test.
	 * 	@param failMessage a message to log if the assertion fails.
	 */
	public static void Assert(bool assertion, string failMessage, object caller) {
		if (!assertion) {
			Debug.LogError(string.Format ("{0}: {1}", caller.GetType(), failMessage));
		}
	}
}