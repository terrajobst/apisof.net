using System.Collections;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Arroyo.Signatures;

namespace Arroyo;

public struct MetadataOperationEnumerator : IEnumerable<MetadataOperation>, IEnumerator<MetadataOperation>
{
    private static readonly object Boxed_i4_M1 = -1;
    private static readonly object Boxed_i4_0 = 0;
    private static readonly object Boxed_i4_1 = 1;
    private static readonly object Boxed_i4_2 = 2;
    private static readonly object Boxed_i4_3 = 3;
    private static readonly object Boxed_i4_4 = 4;
    private static readonly object Boxed_i4_5 = 5;
    private static readonly object Boxed_i4_6 = 6;
    private static readonly object Boxed_i4_7 = 7;
    private static readonly object Boxed_i4_8 = 8;

    private readonly MetadataMethod _method;
    private BlobReader _reader;

    public MetadataOperationEnumerator(MetadataMethod method)
    {
        _method = method;

        var rva = method.RelativeVirtualAddress;
        if (rva == 0 || method.IsExtern)
        {
            _reader = default;
        }
        else
        {
            try
            {
                var block = method.ContainingType.ContainingModule.PEReader.GetMethodBody(rva);
                _reader = block.GetILReader();
            }
            catch (BadImageFormatException)
            {
                _reader = default;
                GlobalStats.IncrementIL();
            }
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)] // Only here to make foreach work
    public MetadataOperationEnumerator GetEnumerator()
    {
        return this;
    }

    IEnumerator<MetadataOperation> IEnumerable<MetadataOperation>.GetEnumerator()
    {
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this;
    }

    public bool MoveNext()
    {
        return _reader.RemainingBytes > 0;
    }

    public MetadataOperation Current
    {
        get
        {
            try
            {
                var opCode = ReadOpCode();
                var argument = ReadArgument(opCode);
                return new MetadataOperation(opCode, argument);
            }
            catch (Exception ex)
            {
                // TODO: Ignore for now;

                _reader = default;
                GlobalStats.IncrementIL();
                GlobalStats.Report(ex);
                return new MetadataOperation(ILOpCode.Nop, null);
            }
        }
    }

    object IEnumerator.Current
    {
        get { return Current; }
    }

    void IEnumerator.Reset()
    {
        throw new NotImplementedException();
    }

    void IDisposable.Dispose()
    {
    }

    private ILOpCode ReadOpCode()
    {
        var result = (int)_reader.ReadByte();
        if (result == 0xFE)
            result = result << 8 | _reader.ReadByte();

        return (ILOpCode)result;
    }

    private object? ReadArgument(ILOpCode opCode)
    {
        object? value = null;

        switch (opCode)
        {
            case ILOpCode.Nop:
            case ILOpCode.Break:
                break;
            case ILOpCode.Ldarg_0:
            case ILOpCode.Ldarg_1:
            case ILOpCode.Ldarg_2:
            case ILOpCode.Ldarg_3:
                value = GetParameter(opCode - ILOpCode.Ldarg_0);
                break;
            case ILOpCode.Ldloc_0:
            case ILOpCode.Ldloc_1:
            case ILOpCode.Ldloc_2:
            case ILOpCode.Ldloc_3:
                value = GetLocal(opCode - ILOpCode.Ldloc_0);
                break;
            case ILOpCode.Stloc_0:
            case ILOpCode.Stloc_1:
            case ILOpCode.Stloc_2:
            case ILOpCode.Stloc_3:
                value = GetLocal(opCode - ILOpCode.Stloc_0);
                break;
            case ILOpCode.Ldarg_s:
            case ILOpCode.Ldarga_s:
            case ILOpCode.Starg_s:
                value = ReadParameter();
                break;
            case ILOpCode.Ldloc_s:
            case ILOpCode.Ldloca_s:
            case ILOpCode.Stloc_s:
                value = ReadLocal();
                break;
            case ILOpCode.Ldnull:
                break;
            case ILOpCode.Ldc_i4_m1:
                value = Boxed_i4_M1;
                break;
            case ILOpCode.Ldc_i4_0:
                value = Boxed_i4_0;
                break;
            case ILOpCode.Ldc_i4_1:
                value = Boxed_i4_1;
                break;
            case ILOpCode.Ldc_i4_2:
                value = Boxed_i4_2;
                break;
            case ILOpCode.Ldc_i4_3:
                value = Boxed_i4_3;
                break;
            case ILOpCode.Ldc_i4_4:
                value = Boxed_i4_4;
                break;
            case ILOpCode.Ldc_i4_5:
                value = Boxed_i4_5;
                break;
            case ILOpCode.Ldc_i4_6:
                value = Boxed_i4_6;
                break;
            case ILOpCode.Ldc_i4_7:
                value = Boxed_i4_7;
                break;
            case ILOpCode.Ldc_i4_8:
                value = Boxed_i4_8;
                break;
            case ILOpCode.Ldc_i4_s:
                value = (int)_reader.ReadSByte();
                break;
            case ILOpCode.Ldc_i4:
                value = _reader.ReadInt32();
                break;
            case ILOpCode.Ldc_i8:
                value = _reader.ReadInt64();
                break;
            case ILOpCode.Ldc_r4:
                value = _reader.ReadSingle();
                break;
            case ILOpCode.Ldc_r8:
                value = _reader.ReadDouble();
                break;
            case ILOpCode.Dup:
            case ILOpCode.Pop:
                break;
            case ILOpCode.Jmp:
                value = ReadMethodReference();
                break;
            case ILOpCode.Call:
                value = ReadMethodReference();
                break;
            case ILOpCode.Calli:
                value = ReadFunctionPointerType();
                break;
            case ILOpCode.Ret:
                break;
            case ILOpCode.Br_s:
            case ILOpCode.Brfalse_s:
            case ILOpCode.Brtrue_s:
            case ILOpCode.Beq_s:
            case ILOpCode.Bge_s:
            case ILOpCode.Bgt_s:
            case ILOpCode.Ble_s:
            case ILOpCode.Blt_s:
            case ILOpCode.Bne_un_s:
            case ILOpCode.Bge_un_s:
            case ILOpCode.Bgt_un_s:
            case ILOpCode.Ble_un_s:
            case ILOpCode.Blt_un_s:
                value = (uint)(_reader.Offset + 1 + _reader.ReadSByte());
                break;
            case ILOpCode.Br:
            case ILOpCode.Brfalse:
            case ILOpCode.Brtrue:
            case ILOpCode.Beq:
            case ILOpCode.Bge:
            case ILOpCode.Bgt:
            case ILOpCode.Ble:
            case ILOpCode.Blt:
            case ILOpCode.Bne_un:
            case ILOpCode.Bge_un:
            case ILOpCode.Bgt_un:
            case ILOpCode.Ble_un:
            case ILOpCode.Blt_un:
                value = (uint)(_reader.Offset + 4 + _reader.ReadInt32());
                break;
            case ILOpCode.Switch:
                value = ReadJumpTable();
                break;
            case ILOpCode.Ldind_i1:
            case ILOpCode.Ldind_u1:
            case ILOpCode.Ldind_i2:
            case ILOpCode.Ldind_u2:
            case ILOpCode.Ldind_i4:
            case ILOpCode.Ldind_u4:
            case ILOpCode.Ldind_i8:
            case ILOpCode.Ldind_i:
            case ILOpCode.Ldind_r4:
            case ILOpCode.Ldind_r8:
            case ILOpCode.Ldind_ref:
            case ILOpCode.Stind_ref:
            case ILOpCode.Stind_i1:
            case ILOpCode.Stind_i2:
            case ILOpCode.Stind_i4:
            case ILOpCode.Stind_i8:
            case ILOpCode.Stind_r4:
            case ILOpCode.Stind_r8:
            case ILOpCode.Add:
            case ILOpCode.Sub:
            case ILOpCode.Mul:
            case ILOpCode.Div:
            case ILOpCode.Div_un:
            case ILOpCode.Rem:
            case ILOpCode.Rem_un:
            case ILOpCode.And:
            case ILOpCode.Or:
            case ILOpCode.Xor:
            case ILOpCode.Shl:
            case ILOpCode.Shr:
            case ILOpCode.Shr_un:
            case ILOpCode.Neg:
            case ILOpCode.Not:
            case ILOpCode.Conv_i1:
            case ILOpCode.Conv_i2:
            case ILOpCode.Conv_i4:
            case ILOpCode.Conv_i8:
            case ILOpCode.Conv_r4:
            case ILOpCode.Conv_r8:
            case ILOpCode.Conv_u4:
            case ILOpCode.Conv_u8:
                break;
            case ILOpCode.Callvirt:
                value = ReadMethodReference();
                break;
            case ILOpCode.Cpobj:
            case ILOpCode.Ldobj:
                value = ReadTypeReference();
                break;
            case ILOpCode.Ldstr:
                value = ReadUserStringForToken();
                break;
            case ILOpCode.Newobj:
                value = ReadMethodReference();
                break;
            case ILOpCode.Castclass:
            case ILOpCode.Isinst:
                value = ReadTypeReference();
                break;
            case ILOpCode.Conv_r_un:
                break;
            case ILOpCode.Unbox:
                value = ReadTypeReference();
                break;
            case ILOpCode.Throw:
                break;
            case ILOpCode.Ldfld:
            case ILOpCode.Ldflda:
            case ILOpCode.Stfld:
                value = ReadFieldReference();
                break;
            case ILOpCode.Ldsfld:
            case ILOpCode.Ldsflda:
            case ILOpCode.Stsfld:
                value = ReadFieldReference();
                break;
            case ILOpCode.Stobj:
                value = ReadTypeReference();
                break;
            case ILOpCode.Conv_ovf_i1_un:
            case ILOpCode.Conv_ovf_i2_un:
            case ILOpCode.Conv_ovf_i4_un:
            case ILOpCode.Conv_ovf_i8_un:
            case ILOpCode.Conv_ovf_u1_un:
            case ILOpCode.Conv_ovf_u2_un:
            case ILOpCode.Conv_ovf_u4_un:
            case ILOpCode.Conv_ovf_u8_un:
            case ILOpCode.Conv_ovf_i_un:
            case ILOpCode.Conv_ovf_u_un:
                break;
            case ILOpCode.Box:
                value = ReadTypeReference();
                break;
            case ILOpCode.Newarr:
                value = ReadTypeReference();
                break;
            case ILOpCode.Ldlen:
                break;
            case ILOpCode.Ldelema:
                value = ReadTypeReference();
                break;
            case ILOpCode.Ldelem_i1:
            case ILOpCode.Ldelem_u1:
            case ILOpCode.Ldelem_i2:
            case ILOpCode.Ldelem_u2:
            case ILOpCode.Ldelem_i4:
            case ILOpCode.Ldelem_u4:
            case ILOpCode.Ldelem_i8:
            case ILOpCode.Ldelem_i:
            case ILOpCode.Ldelem_r4:
            case ILOpCode.Ldelem_r8:
            case ILOpCode.Ldelem_ref:
            case ILOpCode.Stelem_i:
            case ILOpCode.Stelem_i1:
            case ILOpCode.Stelem_i2:
            case ILOpCode.Stelem_i4:
            case ILOpCode.Stelem_i8:
            case ILOpCode.Stelem_r4:
            case ILOpCode.Stelem_r8:
            case ILOpCode.Stelem_ref:
                break;
            case ILOpCode.Ldelem:
                value = ReadTypeReference();
                break;
            case ILOpCode.Stelem:
                value = ReadTypeReference();
                break;
            case ILOpCode.Unbox_any:
                value = ReadTypeReference();
                break;
            case ILOpCode.Conv_ovf_i1:
            case ILOpCode.Conv_ovf_u1:
            case ILOpCode.Conv_ovf_i2:
            case ILOpCode.Conv_ovf_u2:
            case ILOpCode.Conv_ovf_i4:
            case ILOpCode.Conv_ovf_u4:
            case ILOpCode.Conv_ovf_i8:
            case ILOpCode.Conv_ovf_u8:
                break;
            case ILOpCode.Refanyval:
                value = ReadTypeReference();
                break;
            case ILOpCode.Ckfinite:
                break;
            case ILOpCode.Mkrefany:
                value = ReadTypeReference();
                break;
            case ILOpCode.Ldtoken:
                value = ReadRuntimeHandleFromToken();
                break;
            case ILOpCode.Conv_u2:
            case ILOpCode.Conv_u1:
            case ILOpCode.Conv_i:
            case ILOpCode.Conv_ovf_i:
            case ILOpCode.Conv_ovf_u:
            case ILOpCode.Add_ovf:
            case ILOpCode.Add_ovf_un:
            case ILOpCode.Mul_ovf:
            case ILOpCode.Mul_ovf_un:
            case ILOpCode.Sub_ovf:
            case ILOpCode.Sub_ovf_un:
            case ILOpCode.Endfinally:
                break;
            case ILOpCode.Leave:
                value = (uint)(_reader.Offset + 4 + _reader.ReadInt32());
                break;
            case ILOpCode.Leave_s:
                value = (uint)(_reader.Offset + 1 + _reader.ReadSByte());
                break;
            case ILOpCode.Stind_i:
            case ILOpCode.Conv_u:
            case ILOpCode.Arglist:
            case ILOpCode.Ceq:
            case ILOpCode.Cgt:
            case ILOpCode.Cgt_un:
            case ILOpCode.Clt:
            case ILOpCode.Clt_un:
                break;
            case ILOpCode.Ldftn:
            case ILOpCode.Ldvirtftn:
                value = ReadMethodReference();
                break;
            case ILOpCode.Ldarg:
            case ILOpCode.Ldarga:
            case ILOpCode.Starg:
                value = GetParameter(_reader.ReadUInt16());
                break;
            case ILOpCode.Ldloc:
            case ILOpCode.Ldloca:
            case ILOpCode.Stloc:
                value = GetLocal(_reader.ReadUInt16());
                break;
            case ILOpCode.Localloc:
                break;
            case ILOpCode.Endfilter:
                break;
            case ILOpCode.Unaligned:
                value = _reader.ReadByte();
                break;
            case ILOpCode.Volatile:
            case ILOpCode.Tail:
                break;
            case ILOpCode.Initobj:
                value = ReadTypeReference();
                break;
            case ILOpCode.Constrained:
                value = ReadTypeReference();
                break;
            case ILOpCode.Cpblk:
            case ILOpCode.Initblk:
                break;
            case ILOpCode.Rethrow:
                break;
            case ILOpCode.Sizeof:
                value = ReadTypeReference();
                break;
            case ILOpCode.Refanytype:
            case ILOpCode.Readonly:
                break;
            default:
                throw new Exception($"Unexpected opcode {opCode}");
        }

        return value;
    }

    private object ReadRuntimeHandleFromToken()
    {
        var token = _reader.ReadUInt32();
        return token;
    }

    private MetadataItem ReadFieldReference()
    {
        return ReadMemberReference();
    }

    private MetadataItem ReadMemberReference()
    {
        var handle = MetadataTokens.Handle(_reader.ReadInt32());
        return _method.ContainingType.ContainingModule.GetMemberReference(handle);
    }

    private object ReadUserStringForToken()
    {
        var token = _reader.ReadUInt32();
        return token;
    }

    private MetadataType ReadTypeReference()
    {
        var handle = MetadataTokens.Handle(_reader.ReadInt32());
        var context = new MetadataGenericContext(_method);
        return _method.ContainingType.ContainingModule.GetTypeReference(handle, context);
    }

    private object ReadFunctionPointerType()
    {
        var token = _reader.ReadUInt32();
        return token;
    }

    private MetadataItem ReadMethodReference()
    {
        return ReadMemberReference();
    }

    private int ReadLocal()
    {
        var index = _reader.ReadByte();
        return GetLocal(index);
    }

    private int GetLocal(int index)
    {
        return index;
    }

    private int ReadParameter()
    {
        var index = _reader.ReadByte();
        return GetParameter(index);
    }

    private int GetParameter(int index)
    {
        // NOTE: We can't return anything here because we have no representation
        //       of the 'this' parameter.
        return index;
    }

    private uint[] ReadJumpTable()
    {
        var count = _reader.ReadUInt32();
        var result = new uint[count];
        var asOffset = (uint)_reader.Offset + count * 4;
        for (var i = 0; i < count; i++)
            result[i] = _reader.ReadUInt32() + asOffset;

        return result;
    }
}