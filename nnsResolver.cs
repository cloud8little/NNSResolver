using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;

namespace nnsResolver
{
    [DisplayName("nnsResolver")]
    [ManifestExtra("Author", "NEO")]
    [ManifestExtra("Email", "developer@neo.org")]
    [ManifestExtra("Description", "This is a nnsResolver")]
    [ContractPermission("*")]
    public class nnsResolver : SmartContract
    {
        private const byte Prefix_ReverseRecord = 0x01;
        private const byte Prefix_Contract = 0x02;
        private const byte Prefix_Owner = 0x03;

        [InitialValue("NgWhGsfNWdcZFidbu72bHhppyLA7N2inuc", ContractParameterType.Hash160)]
        private static readonly UInt160 InitialOwner = default;

        [InitialValue("NgBFVumLEHH93XMSXQYHEiYR6aoBJRtJLM", ContractParameterType.Hash160)]
        private static readonly UInt160 InitialNNS = default;

        [Safe]
        public static UInt160 Owner()
        {
            if (Storage.Get(Storage.CurrentContext, new byte[] { Prefix_Owner }) is null)
            {
                return InitialOwner;
            }
            return (UInt160)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_Owner });
        }

        public static void SetContract(UInt160 nnsHash)
        {
            ExecutionEngine.Assert(Runtime.CheckWitness(Owner()));
            Storage.Put(Storage.CurrentContext, new byte[] { Prefix_Contract }, nnsHash);
        }

        [Safe]
        public static UInt160 GetContract()
        {
            if (Storage.Get(Storage.CurrentContext, new byte[] { Prefix_Contract }) is null)
            {
                return InitialNNS;
            }
            return (UInt160)Storage.Get(Storage.CurrentContext, new byte[] { Prefix_Contract });
        }

        public static void SetOwner(UInt160 owner)
        {
            ExecutionEngine.Assert(Runtime.CheckWitness(Owner()));
            Storage.Put(Storage.CurrentContext, new byte[] { Prefix_Owner }, owner);
        }

        public static bool SetReverseRecord(UInt160 owner, string name)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            StorageContext context = Storage.CurrentContext;
            StorageMap reverseMap = new(context, Prefix_ReverseRecord);
            try
            {
                string record = (string)Contract.Call(GetContract(), "getRecord", CallFlags.ReadOnly, name, RecordType.TXT);
                if (record == owner.ToAddress(Runtime.AddressVersion))
                {
                    reverseMap.Put(record, name);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                ExecutionEngine.Assert(false, "call getRecord Error.");
                throw;
            }
        }

        [Safe]
        public static string GetReverseRecord(UInt160 owner)
        {
            StorageContext context = Storage.CurrentContext;
            StorageMap reverseMap = new(context, Prefix_ReverseRecord);
            string address = owner.ToAddress(Runtime.AddressVersion);
            if (reverseMap[address] is not null)
            {
                string name = (string)reverseMap[address];
                try
                {
                    string record = (string)Contract.Call(GetContract(), "getRecord", CallFlags.ReadOnly, name, RecordType.TXT);
                    if (record == owner.ToAddress(Runtime.AddressVersion))
                    {
                        return name;
                    }
                    return "";
                }
                catch (Exception)
                {
                    ExecutionEngine.Assert(false, "call getRecord Error.");
                    throw;
                }
            }
            return "";
        }

        public static void ClearReverseRecord(UInt160 owner)
        {
            StorageContext context = Storage.CurrentContext;
            StorageMap reverseMap = new(context, Prefix_ReverseRecord);
            string address = owner.ToAddress(Runtime.AddressVersion);
            string name = GetReverseRecord(owner);
            if (name != "")
            {
                reverseMap.Delete(address);
            }
            else
            {
                ExecutionEngine.Assert(false, "name is not exist.");
            }
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            ExecutionEngine.Assert(Runtime.CheckWitness(Owner()));
            ContractManagement.Update(nefFile, manifest, null);
        }

    }
}
