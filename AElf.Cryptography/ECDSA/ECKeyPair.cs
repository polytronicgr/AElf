﻿using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace AElf.Cryptography.ECDSA
{
    public class ECKeyPair
    {
        public ECPrivateKeyParameters PrivateKey { get; private set; }
        public ECPublicKeyParameters PublicKey { get; private set; }
        public static int AddressLength { get; } = 16;
        
        public ECKeyPair(ECPrivateKeyParameters privateKey, ECPublicKeyParameters publicKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public byte[] GetEncodedPublicKey(bool compressed = false)
        {
            return PublicKey.Q.GetEncoded(compressed);
        }

        public static ECKeyPair FromPublicKey(byte[] publicKey)
        {
            ECPublicKeyParameters pubKey 
                = new ECPublicKeyParameters(Parameters.Curve.Curve.DecodePoint(publicKey), Parameters.DomainParams);
            
            ECKeyPair k = new ECKeyPair(null, pubKey);

            return k;
        }

        public byte[] GetAddress()
        {
            return SHA256.Create().ComputeHash(GetEncodedPublicKey()).Take(AddressLength).ToArray();
        }

        public string GetHexaAddress()
        {
            return BitConverter.ToString(this.GetAddress()).Replace("-","");
        }
    }
}