using AutoMapper;
using HotelPOS.Application.DTOs.Category;
using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ── Item ──────────────────────────────────────────────────────────
            CreateMap<CreateItemDto, CreateItemCommand>();
            CreateMap<CreateItemDto, Item>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));
            CreateMap<CreateItemDto, UpdateItemCommand>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()); // Id comes from route

            // ── Table ─────────────────────────────────────────────────────────
            CreateMap<CreateTableDto, Table>();

            // ── Category ─────────────────────────────────────────────────────
            CreateMap<SaveCategoryDto, Category>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));

            // ── Supplier ──────────────────────────────────────────────────────
            CreateMap<SaveSupplierDto, Supplier>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Gstin, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Gstin) ? null : src.Gstin.Trim().ToUpperInvariant()));
            CreateMap<Supplier, SaveSupplierDto>();
        }
    }
}
