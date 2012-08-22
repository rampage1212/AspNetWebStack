﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Security;
using Microsoft.TestCommon;

namespace System.Web.Helpers.AntiXsrf.Test
{
    public class MachineKey40CryptoSystemTest
    {
        private static readonly MachineKey40CryptoSystem _dummyCryptoSystem = new MachineKey40CryptoSystem(HexEncoder, HexDecoder);

        [Fact]
        public void Base64ToHex()
        {
            // Arrange
            string base64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_0";
            string hex = "00108310518720928B30D38F41149351559761969B71D79F8218A39259A7A29AABB2DBAFC31CB3D35DB7E39EBBF3DFBF";

            // Act
            string retVal = MachineKey40CryptoSystem.Base64ToHex(base64);

            // Assert
            Assert.Equal(hex, retVal);
        }

        [Fact]
        public void Base64ToHex_HexToBase64_RoundTrips()
        {
            for (int i = 0; i <= Byte.MaxValue; i++)
            {
                // Arrange
                string hex = String.Format("{0:X2}", i);

                // Act
                string retVal = MachineKey40CryptoSystem.Base64ToHex(MachineKey40CryptoSystem.HexToBase64(hex));

                // Assert
                Assert.Equal(hex, retVal);
            }
        }

        [Fact]
        public void HexToBase64()
        {
            // Arrange
            string base64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_0";
            string hex = "00108310518720928B30D38F41149351559761969B71D79F8218A39259A7A29AABB2DBAFC31CB3D35DB7E39EBBF3DFBF";

            // Act
            string retVal = MachineKey40CryptoSystem.HexToBase64(hex);

            // Assert
            Assert.Equal(base64, retVal);
        }

        [Fact]
        public void Protect()
        {
            // Arrange
            byte[] data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act
            string retVal = _dummyCryptoSystem.Protect(data);

            // Assert
            Assert.Equal("hYfyZgECAwQF0", retVal);
        }

        [Theory]
        [InlineData("hYfyZwECAwQF0")] // bad MagicHeader
        [InlineData("hYfy0")] // too short to contain MagicHeader
        public void Unprotect_Failure(string protectedData)
        {
            // Act
            byte[] retVal = _dummyCryptoSystem.Unprotect(protectedData);

            // Assert
            Assert.Null(retVal);
        }

        [Fact]
        public void Unprotect_Success()
        {
            // Arrange
            byte[] expected = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act
            byte[] retVal = _dummyCryptoSystem.Unprotect("hYfyZgECAwQF0");

            // Assert
            Assert.Equal(expected, retVal);
        }

        [Fact]
        public void Protect_Unprotect_RoundTrips()
        {
            // Arrange
            byte[] data = new byte[1024];
            new Random().NextBytes(data);

            // Act
            byte[] roundTripped = _dummyCryptoSystem.Unprotect(_dummyCryptoSystem.Protect(data));

            // Assert
            Assert.Equal(data, roundTripped);
        }

        private static string HexEncoder(byte[] data, MachineKeyProtection protection)
        {
            Assert.Equal(MachineKeyProtection.All, protection);
            return HexUtil.HexEncode(data);
        }

        private static byte[] HexDecoder(string input, MachineKeyProtection protection)
        {
            Assert.Equal(MachineKeyProtection.All, protection);
            return HexUtil.HexDecode(input);
        }
    }
}
