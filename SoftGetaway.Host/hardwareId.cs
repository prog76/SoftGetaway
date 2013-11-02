using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace softGetaway {

    internal class HardwareHelper {
        public string GetHardwareID() {
            // ABCDEF GH
            var vsn = getVolumeSerial(Environment.SystemDirectory.Substring(0, 1));
            var result = getHash(vsn + getDiskSize().ToString()).Substring(2, 6);
            result += getHash(result).Substring(5, 2);
            result = separateText(result, 4, @"-");

            return result;
        }

        private string getHash(string s) {
            return encodeText(getHashValue(s));
        }

        private string encodeText(string s) {
            string buf = s.Replace(Convert.ToChar(10).ToString(),
                                                            string.Empty);
            string bin = string.Empty;
            for (int i = 0; i < buf.Length; i++) {
                bin += intToBin(Convert.ToByte(buf[i]),
                                                 7);
            }

            var result = string.Empty;
            while (bin.Length > 0) {
                string w5 = safeSubstring(bin,
                                                                   0,
                                                                   5);
                w5 = w5.PadRight(5,
                                                  '0');

                result += bin5ToB36(w5);
                bin = safeSubstring(bin,
                                                         5,
                                                         0);
            }

            return result;
        }

        public HardwareHelper() {
            _alphabet = new char[36];

            for (int i = 0; i < 26; i++)
                _alphabet[i] = Convert.ToChar(i + Convert.ToInt16('A'));
            for (int i = 0; i < 10; i++)
                _alphabet[i + 26] = Convert.ToChar(i + Convert.ToInt16('0'));
        }

        private char bin5ToB36(string w5) {
            int d = Convert.ToInt16(w5, 2);
            return _alphabet[d];
        }

        private readonly char[] _alphabet;

        private static string intToBin(
                int number,
                int numBits) {
            var b = new char[numBits];
            var pos = numBits - 1;

            var i = 0;
            while (i < numBits) {
                if ((number & (1 << i)) != 0) {
                    b[pos] = '1';
                } else {
                    b[pos] = '0';
                }
                pos--;
                i++;
            }

            return new string(b);
        }

        private static string safeSubstring(
                string s,
                int start,
                int length) {
            if (length == 0)
                length = s.Length - start;
            if (length < 0)
                length = 0;

            if (start > s.Length)
                return string.Empty;
            if (start < 0)
                start = 0;

            if (start + length >
                     s.Length)
                return s.Substring(start);

            return s.Substring(start, length);
        }

        private static string separateText(
                string s,
                int numChars,
                string separator) {
            var result = string.Empty;
            var count = 0;
            for (var i = 0; i < s.Length; i++) {
                if ((count != 0) && (count % numChars == 0)) {
                    result += separator;
                }

                result += s[i];
                count++;
            }

            return result;
        }

        private static string getHashValue(string s) {
            const string def = @"5CACAD0D1C88626D74B30C1ADC2951E8";

            if (string.IsNullOrEmpty(s)) {
                return def;
            } else {
                MD5 md5 = new MD5CryptoServiceProvider();
                var textToHash = Encoding.Default.GetBytes(s);
                var h = md5.ComputeHash(textToHash);

                var result = BitConverter.ToString(h);
                result = result.Replace(@"-", string.Empty);
                result = result.ToUpperInvariant();

                return result;
            }
        }

        private static int getVolumeSerial(string drive) {
            var voln = new StringBuilder(300);
            int vsn;
            uint mcl, fsf;
            var fsnb = new StringBuilder(300);
            GetVolumeInformation(string.Format(@"{0}:\", drive),
                                                      voln,
                                                      voln.Capacity,
                                                      out vsn,
                                                      out mcl,
                                                      out fsf,
                                                      fsnb,
                                                      fsnb.Capacity);

            return vsn;
        }

        private static ulong getDiskSize() {
            ulong freeBytesAvail;
            ulong totalNumOfBytes;
            ulong totalNumOfFreeBytes;

            GetDiskFreeSpaceEx(
                    string.Format(
                    @"{0}:\",
                    Environment.SystemDirectory.Substring(0, 1)),
                                                    out freeBytesAvail,
                                                    out totalNumOfBytes,
                                                    out totalNumOfFreeBytes);

            return totalNumOfBytes;
        }

        [DllImport(@"kernel32.dll")]
        private static extern bool GetDiskFreeSpaceEx(
                string lpDirectoryName,
                out ulong lpFreeBytesAvailable,
                out ulong lpTotalNumberOfBytes,
                out ulong lpTotalNumberOfFreeBytes);

        [DllImport(@"Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetVolumeInformation(
                string rootPathName,
                StringBuilder volumeNameBuffer,
                int volumeNameSize,
                out int volumeSerialNumber,
                out uint maximumComponentLength,
                out uint fileSystemFlags,
                StringBuilder fileSystemNameBuffer,
                int nFileSystemNameSize);

    }
}

