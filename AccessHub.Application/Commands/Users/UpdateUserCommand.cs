public class UpdateUserCommand : IRequest
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        var user = await _userRepository.GetByIdAsync(new UserId(request.UserId));
        if (user == null)
            throw new NotFoundException($"User {request.UserId} not found");

        user.UpdateEmail(request.Email);
        user.UpdatePhoneNumber(request.PhoneNumber);

        //await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitTransactionAsync();
    }
}