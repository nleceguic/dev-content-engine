using DevContentEngine.Domain.Common;
using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Domain.Events;

public sealed record ContentIdeaSelectedEvent(Guid ContentIdeaId, ContentOrigin Origin, ContentPath ChosenPath) : DomainEvent;
