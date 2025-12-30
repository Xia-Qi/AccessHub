public class DeleteUserCommand : IRequest
{
    public Guid UserId { get; set; }
}

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        var user = await _userRepository.GetByIdAsync(new UserId(request.UserId));
        if (user == null)
            throw new NotFoundException($"User {request.UserId} not found");

        user.SoftDelete();

        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitTransactionAsync();
    }
}