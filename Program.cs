using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

namespace _08._10._24
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (var db = new ApplicationContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var guest1 = new Guest { Name = "Jeff" };
                var guest2 = new Guest { Name = "Jane" };
                var event1 = new Event { Name = "Team meeting" };
                var event2 = new Event { Name = "Business Summit" };

                db.Guests.AddRange(guest1, guest2);
                db.Events.AddRange(event1, event2);
                db.SaveChanges();

                db.AddGuestToEvent(guest1.Id, event1.Id, "Speaker");
                db.AddGuestToEvent(guest2.Id, event1.Id, "Attendee");
                db.AddGuestToEvent(guest1.Id, event2.Id, "Attendee");

                db.SaveChanges();

                var guestsOnEvent = db.GetGuestsOnEvent(event1.Id);
                var eventsForGuest = db.GetEventsForGuest(guest1.Id);
                db.ChangeGuestRole(guest1.Id, event1.Id, "Moderator");
                var speakerEvents = db.GetEventsForRole("Speaker");
                var topGuests = db.GetTopGuests(3);
            }
        }
    }
    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<GuestEvent> GuestEvents { get; set; } = new List<GuestEvent>();
    }
    public class Guest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<GuestEvent> GuestEvents { get; set; } = new List<GuestEvent>();
    }
    public class GuestEvent
    {
        public int GuestId { get; set; }
        public Guest Guest { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }

        public string Role { get; set; }
    }
    public class ApplicationContext : DbContext
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<GuestEvent> GuestEvents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\ProjectModels;Database=EventGuestDb;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuestEvent>().HasKey(ge => new { ge.GuestId, ge.EventId });

            modelBuilder.Entity<GuestEvent>()
                .HasOne(ge => ge.Guest)
                .WithMany(g => g.GuestEvents)
                .HasForeignKey(ge => ge.GuestId);

            modelBuilder.Entity<GuestEvent>()
                .HasOne(ge => ge.Event)
                .WithMany(e => e.GuestEvents)
                .HasForeignKey(ge => ge.EventId);
        }

        // 1
        public void AddGuestToEvent(int guestId, int eventId, string role)
        {
            GuestEvents.Add(new GuestEvent { GuestId = guestId, EventId = eventId, Role = role });
        }

        // 2
        public List<Guest> GetGuestsOnEvent(int eventId)
        {
            return GuestEvents
                .Where(ge => ge.EventId == eventId)
                .Select(ge => ge.Guest)
                .ToList();
        }

        // 3
        public void ChangeGuestRole(int guestId, int eventId, string newRole)
        {
            var guestEvent = GuestEvents.SingleOrDefault(ge => ge.GuestId == guestId && ge.EventId == eventId);
            if (guestEvent != null)
            {
                guestEvent.Role = newRole;
                SaveChanges();
            }
        }

        // 4
        public List<Event> GetEventsForGuest(int guestId)
        {
            return GuestEvents
                .Where(ge => ge.GuestId == guestId)
                .Select(ge => ge.Event)
                .ToList();
        }

        // 5
        public void RemoveGuestFromEvent(int guestId, int eventId)
        {
            var guestEvent = GuestEvents.SingleOrDefault(ge => ge.GuestId == guestId && ge.EventId == eventId);
            if (guestEvent != null)
            {
                GuestEvents.Remove(guestEvent);
                SaveChanges();
            }
        }

        // 6
        public List<Event> GetEventsForRole(string role)
        {
            return GuestEvents
                .Where(ge => ge.Role == role)
                .Select(ge => ge.Event)
                .ToList();
        }

        // 7
        public List<(Guest guest, int eventCount, List<Event> events)> GetTopGuests(int top)
        {
            return GuestEvents
                .GroupBy(ge => ge.Guest)
                .OrderByDescending(g => g.Count())
                .Take(top)
                .Select(g => (guest: g.Key, eventCount: g.Count(), events: g.Select(ge => ge.Event).ToList()))
                .ToList();
        }
    }
}
