#nullable enable

using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Estimations.Commands;
using HotelPOS.Application.UseCases.Estimations.Queries;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases
{
    public class EstimationService : IEstimationService
    {
        private readonly IMediator? _mediator;
        private readonly IEstimationRepository? _estimationRepository;
        private readonly IOrderService? _orderService;
        private readonly IAuthorizationService? _authorization;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public EstimationService(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Legacy constructor for unit tests that inject repositories directly.</summary>
        public EstimationService(IEstimationRepository estimationRepository, IOrderService orderService, IAuthorizationService authorization)
        {
            _estimationRepository = estimationRepository;
            _orderService = orderService;
            _authorization = authorization;
        }

        public async Task<List<Estimation>> GetEstimationsAsync()
        {
            if (_mediator != null)
                return await _mediator.Send(new GetAllEstimationsQuery());

            return await _estimationRepository!.GetEstimationsAsync();
        }

        public async Task<Estimation?> GetEstimationByIdAsync(int id)
        {
            if (_mediator != null)
                return await _mediator.Send(new GetEstimationByIdQuery(id));

            return await _estimationRepository!.GetByIdAsync(id);
        }

        public async Task SaveEstimationAsync(Estimation estimation)
        {
            if (estimation == null) throw new ArgumentNullException(nameof(estimation));

            if (_mediator != null)
            {
                await _mediator.Send(new SaveEstimationCommand(estimation));
                await _mediator.Publish(new EntityActionEvent("Estimation", estimation.Id, "Create",
                    $"Number: {estimation.EstimationNumber}, Items: {estimation.EstimationItems.Count}"));
                return;
            }

            var handler = new SaveEstimationCommandHandler(_estimationRepository!, _authorization!);
            await handler.Handle(new SaveEstimationCommand(estimation), CancellationToken.None);
        }

        public async Task UpdateEstimationAsync(Estimation estimation)
        {
            if (estimation == null) throw new ArgumentNullException(nameof(estimation));

            if (_mediator != null)
            {
                await _mediator.Send(new UpdateEstimationCommand(estimation));
                await _mediator.Publish(new EntityActionEvent("Estimation", estimation.Id, "Update",
                    $"Number: {estimation.EstimationNumber}, Items: {estimation.EstimationItems.Count}"));
                return;
            }

            var handler = new UpdateEstimationCommandHandler(_estimationRepository!, _authorization!);
            await handler.Handle(new UpdateEstimationCommand(estimation), CancellationToken.None);
        }

        public async Task DeleteEstimationAsync(int id)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new DeleteEstimationCommand(id));
                await _mediator.Publish(new EntityActionEvent("Estimation", id, "Delete", "Deleted"));
                return;
            }

            var handler = new DeleteEstimationCommandHandler(_estimationRepository!, _authorization!);
            await handler.Handle(new DeleteEstimationCommand(id), CancellationToken.None);
        }

        public async Task<int> ConvertToOrderAsync(int estimationId)
        {
            if (_mediator != null)
            {
                var orderId = await _mediator.Send(new ConvertEstimationToOrderCommand(estimationId));
                await _mediator.Publish(new EntityActionEvent("Estimation", estimationId, "ConvertToOrder", $"OrderId: {orderId}"));
                return orderId;
            }

            var handler = new ConvertEstimationToOrderCommandHandler(_estimationRepository!, _orderService!, _authorization!);
            return await handler.Handle(new ConvertEstimationToOrderCommand(estimationId), CancellationToken.None);
        }
    }
}
