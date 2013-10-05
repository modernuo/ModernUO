/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace Server {
	/// <summary>
	/// Handles random number generation.
	/// </summary>
	public static class RandomImpl {
		private static readonly IRandomImpl _Random;

		static RandomImpl() {
			if ( Core.Unix ) {
				_Random = new SimpleRandom();
			} else if (Core.Is64Bit && File.Exists("rdrand64.dll")) {
				_Random = new RDRand64();
			} else if ( !Core.Is64Bit && File.Exists("rdrand32.dll") ) {
				_Random = new RDRand32();
			} else {
				_Random = new CSPRandom();
			}

			if (_Random is IHardwareRNG) {
				if (!((IHardwareRNG)_Random).IsSupported()) {
					_Random = new CSPRandom();
				}
			}
		}

		public static double NextDouble() {
			return _Random.NextDouble();
		}

		public static int Next(int c) {
			return _Random.Next(c);
		}

		public static void NextBytes(byte[] b) {
			_Random.NextBytes(b);
		}
	}

	public interface IRandomImpl {
		double NextDouble();
		int Next(int c);
		void NextBytes(byte[] b);
	}

	public interface IHardwareRNG {
		bool IsSupported();
	}

	public sealed class SimpleRandom : IRandomImpl {
		private Random m_Random = new Random();

		public SimpleRandom() {
		}

		public double NextDouble() {
			double r;
			lock (m_Random)
				r = m_Random.NextDouble();
			return r;
		}

		public int Next(int c) {
			int r;
			lock (m_Random)
				r = m_Random.Next(c);
			return r;
		}

		public void NextBytes(byte[] b) {
			lock (m_Random)
				m_Random.NextBytes(b);
		}
	}

	public sealed class CSPRandom : IRandomImpl {
		private RNGCryptoServiceProvider _CSP = new RNGCryptoServiceProvider();

		private static int BUFFER_SIZE = 0x4000;
		private static int LARGE_REQUEST = 0x40;

		private byte[] _Working = new byte[BUFFER_SIZE];
		private byte[] _Buffer = new byte[BUFFER_SIZE];

		private int _Index = 0;

		private object _sync = new object();

		public CSPRandom() {
			_CSP.GetBytes(_Working);
			ThreadPool.QueueUserWorkItem(new WaitCallback(Fill));
		}

		private void CheckSwap(int c) {
			lock (_sync) {
				if (_Index + c < BUFFER_SIZE)
					return;

				lock (_Buffer) {
					byte[] b = _Working;
					_Working = _Buffer;
					_Buffer = b;
					_Index = 0;
				}
			}
			ThreadPool.QueueUserWorkItem(new WaitCallback(Fill));
		}

		private void Fill(object o) {
			lock (_Buffer)
				lock (_CSP)
					_CSP.GetBytes(_Buffer);
		}

		private void _GetBytes(byte[] b) {
			int c = b.Length;

			CheckSwap(c);

			lock (_sync) {
				Buffer.BlockCopy(_Working, _Index, b, 0, c);
				_Index += c;
			}
		}

		public double NextDouble() {
			byte[] b = new byte[8];

			_GetBytes(b);

			return (double)BitConverter.ToUInt64(b, 0) / ulong.MaxValue;
		}

		public int Next(int c) {
			return (int)(c * NextDouble());
		}

		public void NextBytes(byte[] b) {
			int c = b.Length;

			if (c >= LARGE_REQUEST) {
				lock (_CSP)
					_CSP.GetBytes(b);
				return;
			}
			_GetBytes(b);
		}
	}

	public sealed class RDRand32 : IRandomImpl, IHardwareRNG {
		[DllImport("rdrand32")]
		private static extern RDRandError rdrand_32(ref uint rand, bool retry);

		[DllImport("rdrand32")]
		private static extern RDRandError rdrand_get_bytes(int n, byte[] buffer);

		private static int BUFFER_SIZE = 0x10000;
		private static int LARGE_REQUEST = 0x40;

		private byte[] _Working = new byte[BUFFER_SIZE];
		private byte[] _Buffer = new byte[BUFFER_SIZE];

		private int _Index = 0;

		private object _sync = new object();

		public RDRand32() {
			rdrand_get_bytes(BUFFER_SIZE, _Working);
			ThreadPool.QueueUserWorkItem(new WaitCallback(Fill));
		}

		public bool IsSupported() {
			uint r = 0;
			return rdrand_32(ref r, true) == RDRandError.Success;
		}

		private void CheckSwap(int c) {
			lock (_sync) {
				if (_Index + c < BUFFER_SIZE)
					return;

				lock (_Buffer) {
					byte[] b = _Working;
					_Working = _Buffer;
					_Buffer = b;
					_Index = 0;
				}
			}
			ThreadPool.QueueUserWorkItem(new WaitCallback(Fill));
		}

		private void Fill(object o) {
			lock (_Buffer)
				rdrand_get_bytes(BUFFER_SIZE, _Buffer);
		}

		private void _GetBytes(byte[] b) {
			int c = b.Length;

			CheckSwap(c);

			lock (_sync) {
				Buffer.BlockCopy(_Working, _Index, b, 0, c);
				_Index += c;
			}
		}

		public double NextDouble() {
			byte[] b = new byte[8];
			_GetBytes(b);
			return (double)BitConverter.ToUInt64(b, 0) / ulong.MaxValue;
		}

		public int Next(int c) {
			return (int)(c * NextDouble());
		}

		public void NextBytes(byte[] b) {
			int c = b.Length;

			if (c >= LARGE_REQUEST) {
				rdrand_get_bytes(c, b);
				return;
			}
			_GetBytes(b);
		}
	}

	public sealed class RDRand64 : IRandomImpl, IHardwareRNG {
		[DllImport("rdrand64")]
		private static extern RDRandError rdrand_64(ref ulong rand, bool retry);

		[DllImport("rdrand64")]
		private static extern RDRandError rdrand_get_bytes(int n, byte[] buffer);

		private static int BUFFER_SIZE = 0x10000;
		private static int LARGE_REQUEST = 0x40;

		private byte[] _Working = new byte[BUFFER_SIZE];
		private byte[] _Buffer = new byte[BUFFER_SIZE];

		private int _Index = 0;

		private object _sync = new object();

		public RDRand64() {
			rdrand_get_bytes(BUFFER_SIZE, _Working);
			ThreadPool.QueueUserWorkItem(new WaitCallback(Fill));
		}

		public bool IsSupported() {
			ulong r = 0;
			return rdrand_64(ref r, true) == RDRandError.Success;
		}

		private void CheckSwap(int c) {
			lock (_sync) {
				if (_Index + c < BUFFER_SIZE)
					return;

				lock (_Buffer) {
					byte[] b = _Working;
					_Working = _Buffer;
					_Buffer = b;
					_Index = 0;
				}
			}
			ThreadPool.QueueUserWorkItem(new WaitCallback(Fill));
		}

		private void Fill(object o) {
			lock (_Buffer)
				rdrand_get_bytes(BUFFER_SIZE, _Buffer);
		}

		private void _GetBytes(byte[] b) {
			int c = b.Length;

			CheckSwap(c);

			lock (_sync) {
				Buffer.BlockCopy(_Working, _Index, b, 0, c);
				_Index += c;
			}
		}

		public double NextDouble() {
			byte[] b = new byte[8];
			_GetBytes(b);
			return (double)BitConverter.ToUInt64(b, 0) / ulong.MaxValue;
		}

		public int Next(int c) {
			return (int)(c * NextDouble());
		}

		public void NextBytes(byte[] b) {
			int c = b.Length;

			if (c >= LARGE_REQUEST) {
				rdrand_get_bytes(c, b);
				return;
			}
			_GetBytes(b);
		}
	}

	public enum RDRandError : int {
		Unknown = -4,
		Unsupported = -3,
		Supported = -2,
		NotReady = -1,

		Failure = 0,

		Success = 1,
	}
}