using System.ComponentModel.DataAnnotations;
using Assignment.ViewModels;

namespace Assignment.Tests;

public class ValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Booking_form_requires_check_out_after_check_in()
    {
        var model = new CreateReservationViewModel
        {
            From = DateTime.Now.AddDays(2), To = DateTime.Now.AddDays(1), RoomId = Guid.NewGuid(), GuestName = "A",
        };
        Assert.Contains(Validate(model), r => r.MemberNames.Contains("To"));
    }

    [Fact]
    public void Booking_form_rejects_past_dates()
    {
        var model = new CreateReservationViewModel { From = DateTime.Now.AddDays(-1), To = DateTime.Now.AddDays(1), RoomId = Guid.NewGuid(), GuestName = "A" };
        Assert.Contains(Validate(model), r => r.MemberNames.Contains("From"));
    }

    [Fact]
    public void Booking_form_needs_an_existing_or_new_guest()
    {
        var model = new CreateReservationViewModel { RoomId = Guid.NewGuid() };
        Assert.Contains(Validate(model), r => r.MemberNames.Contains("GuestName"));
    }

    [Fact]
    public void Valid_booking_form_passes()
    {
        var model = new CreateReservationViewModel { RoomId = Guid.NewGuid(), CustomerId = Guid.NewGuid() };
        Assert.Empty(Validate(model));
    }
}
