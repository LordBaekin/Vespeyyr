// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Entities;
    using Serializer;
    using Brook;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Brook.Octet;
    using UnityEngine;
    using Log;
    using Logger = Log.Logger;

    public static class GenericNetworkCommandArgs
    {
        public static readonly int MAX_ENTITY_REFS = 4;
        public static readonly int BYTE_LIST_OVERHEAD = 2;
        public static readonly int MAX_BYTE_ARRAY_LENGTH = OutProtocolBitStream.BYTES_LIST_MAX_LENGTH - BYTE_LIST_OVERHEAD;

        private const int BITS_IN_BYTE = 8;
        private const int QUATERNION_BITS_PER_COMPONENT = 32;

        private static readonly FloatMeta ColorFloatMeta = FloatMeta.ForFixedPoint(0, 1, 1 / 255d);
        private static readonly FloatMeta NoCompression = FloatMeta.NoCompression();

        public static (byte[], Entity[]) SerializeCommandArgs(MethodInfo method, CoherenceBridge bridge, object[] args, Logger logger)
        {
            // If sending just a byte array, we need to add 2 bytes for the null flag and the length,
            // so we make sure there's that overhead.
            var octetWriter = new OutOctetStream(OutProtocolBitStream.BYTES_LIST_MAX_LENGTH);
            var bitStream = new OutBitStream(octetWriter);
            var outStream = OutProtocolBitStream.Shared.Reset(bitStream, logger);

            // Entity IDs are translated by the RS, so they can't be serialized.
            // Instead, they are returned as an array.
            var sIDs = new List<Entity>();

            foreach (var arg in args)
            {
                var oldPosition = bitStream.Position;

                switch (arg)
                {
                    case null:
                        // write a 1 bit to indicate null.
                        // all nullable types read the bit first and then decide
                        // to read the actual value or not.
                        outStream.WriteBool(true);
                        break;
                    case bool b:
                        outStream.WriteBool(b);
                        break;
                    case byte[] ba:
                        if (ba.Length > MAX_BYTE_ARRAY_LENGTH)
                        {
                            logger.Error(Error.ToolkitSyncCommandGenericByteArgTooLong,
                                $"Byte array command argument for {method.Name} is too long: {ba.Length} bytes. Max is {MAX_BYTE_ARRAY_LENGTH} bytes.");
                            return (null, null);
                        }

                        outStream.WriteBool(false);
                        if (bitStream.IsFull)
                        {
                            logger.Error(Error.ToolkitSyncCommandGenericArgsTooBig,
                                ("method", method.Name));

                            return (null, null);
                        }

                        outStream.WriteBytesList(ba);
                        break;
                    case GameObject go:
                        outStream.WriteBool(false);
                        sIDs.Add(bridge.UnityObjectToEntityId(go));
                        break;
                    case Transform t:
                        outStream.WriteBool(false);
                        sIDs.Add(bridge.UnityObjectToEntityId(t));
                        break;
                    case CoherenceSync s:
                        outStream.WriteBool(false);
                        sIDs.Add(bridge.UnityObjectToEntityId(s));
                        break;
                    case float f:
                        outStream.WriteFloat(f, NoCompression);
                        break;
                    case double d:
                        outStream.WriteDouble(d);
                        break;
                    case int i:
                        outStream.WriteIntegerRange(i, sizeof(int) * BITS_IN_BYTE, 0);
                        break;
                    case uint ui:
                        outStream.WriteUIntegerRange(ui, sizeof(uint) * BITS_IN_BYTE, 0);
                        break;
                    case short s:
                        outStream.WriteShort(s);
                        break;
                    case ushort us:
                        outStream.WriteUShort(us);
                        break;
                    case byte b:
                        outStream.WriteByte(b);
                        break;
                    case sbyte sb:
                        outStream.WriteSByte(sb);
                        break;
                    case char c:
                        outStream.WriteChar(c);
                        break;
                    case long l:
                        outStream.WriteLong(l);
                        break;
                    case ulong ul:
                        outStream.WriteULong(ul);
                        break;
                    case Quaternion q:
                        outStream.WriteQuaternion(q.ToCoreQuaternion(), QUATERNION_BITS_PER_COMPONENT);
                        break;
                    case Color c:
                        outStream.WriteVector4(c.ToCoreColor(), ColorFloatMeta);
                        break;
                    case string s:
                        outStream.WriteBool(false);
                        if (bitStream.IsFull)
                        {
                            logger.Error(Error.ToolkitSyncCommandGenericArgsTooBig,
                                ("method", method.Name));

                            return (null, null);
                        }

                        outStream.WriteShortString(s);
                        break;
                    case Vector2 v2:
                        outStream.WriteVector2(v2.ToCoreVector2(), NoCompression);
                        break;
                    case Vector3 v3:
                        outStream.WriteVector3(v3.ToCoreVector3(), NoCompression);
                        break;
                    default:
                        logger.Error(Error.ToolkitSyncCommandGenericUnsupportedArgType,
                            $"Can't send {arg.GetType()} as parameter in Command.");
                        break;
                }

                if (bitStream.IsFull && oldPosition == bitStream.Position)
                {
                    logger.Error(Error.ToolkitSyncCommandGenericArgsTooBig,
                        ("method", method.Name));

                    return (null, null);
                }
            }

            bitStream.Flush();

            return (octetWriter.Octets.ToArray(), sIDs.ToArray());
        }

        public static object[] DeserializeCommandArgs(MethodInfo method, ICoherenceBridge bridge, byte[] data, Entity[] entityIDs, Logger logger)
        {
            var methodArgs = new List<object>();

            var octetReader = new InOctetStream(data);
            var bitStream = new InBitStream(octetReader, data.Length * 8);
            var inStream = new InProtocolBitStream(bitStream);

            // EntityIDs can't be encoded in the data, they are translated by the RS, so they're
            // separated.
            var entityIDIndex = 0;

            foreach (var parameter in method.GetParameters())
            {
                if (parameter.ParameterType == typeof(bool))
                {
                    methodArgs.Add(inStream.ReadBool());
                }
                else if (parameter.ParameterType == typeof(byte[]))
                {
                    var isNull = inStream.ReadBool();
                    methodArgs.Add(isNull ? Array.Empty<Byte>() : inStream.ReadBytesList());
                }
                else if (parameter.ParameterType == typeof(GameObject))
                {
                    var isNull = inStream.ReadBool();
                    methodArgs.Add(isNull ? null : bridge.EntityIdToGameObject(entityIDs[entityIDIndex++]));
                }
                else if (parameter.ParameterType == typeof(Transform))
                {
                    var isNull = inStream.ReadBool();
                    methodArgs.Add(isNull ? null : bridge.EntityIdToTransform(entityIDs[entityIDIndex++]));
                }
                else if (parameter.ParameterType == typeof(CoherenceSync))
                {
                    var isNull = inStream.ReadBool();
                    methodArgs.Add(isNull ? null : bridge.EntityIdToCoherenceSync(entityIDs[entityIDIndex++]));
                }
                else if (parameter.ParameterType == typeof(float))
                {
                    methodArgs.Add(inStream.ReadFloat(NoCompression));
                }
                else if (parameter.ParameterType == typeof(double))
                {
                    methodArgs.Add(inStream.ReadDouble());
                }
                else if (parameter.ParameterType == typeof(int))
                {
                    methodArgs.Add(inStream.ReadIntegerRange(sizeof(int) * BITS_IN_BYTE, 0));
                }
                else if (parameter.ParameterType == typeof(uint))
                {
                    methodArgs.Add(inStream.ReadUIntegerRange(sizeof(uint) * BITS_IN_BYTE, 0));
                }
                else if (parameter.ParameterType == typeof(short))
                {
                    methodArgs.Add(inStream.ReadShort());
                }
                else if (parameter.ParameterType == typeof(ushort))
                {
                    methodArgs.Add(inStream.ReadUShort());
                }
                else if (parameter.ParameterType == typeof(char))
                {
                    methodArgs.Add(inStream.ReadChar());
                }
                else if (parameter.ParameterType == typeof(byte))
                {
                    methodArgs.Add(inStream.ReadByte());
                }
                else if (parameter.ParameterType == typeof(sbyte))
                {
                    methodArgs.Add(inStream.ReadSByte());
                }
                else if (parameter.ParameterType == typeof(long))
                {
                    methodArgs.Add(inStream.ReadLong());
                }
                else if (parameter.ParameterType == typeof(ulong))
                {
                    methodArgs.Add(inStream.ReadULong());
                }
                else if (parameter.ParameterType == typeof(Quaternion))
                {
                    methodArgs.Add(inStream.ReadQuaternion(QUATERNION_BITS_PER_COMPONENT).ToUnityQuaternion());
                }
                else if (parameter.ParameterType == typeof(Color))
                {
                    methodArgs.Add(inStream.ReadVector4(ColorFloatMeta).ToUnityColor());
                }
                else if (parameter.ParameterType == typeof(string))
                {
                    var isNull = inStream.ReadBool();
                    methodArgs.Add(isNull ? string.Empty : inStream.ReadShortString());
                }
                else if (parameter.ParameterType == typeof(Vector2))
                {
                    methodArgs.Add(inStream.ReadVector2(NoCompression).ToUnityVector2());
                }
                else if (parameter.ParameterType == typeof(Vector3))
                {
                    methodArgs.Add(inStream.ReadVector3(NoCompression).ToUnityVector3());
                }
                else
                {
                    logger.Error(Error.ToolkitSyncCommandGenericUnsupportedArgType,
                        $"Command can't call method with argument of type '{parameter.ParameterType}'.");
                    return null;
                }
            }

            return methodArgs.ToArray();
        }
    }
}
