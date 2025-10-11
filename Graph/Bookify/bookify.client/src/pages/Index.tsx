import { useState, useEffect } from "react";
import { MeetingRoomCard } from "../components/MeetingRoomCard";
import { BookingDialog } from "../components/BookingDialog";
import { RoomBookingsDialog } from "../components/RoomBookingsDialog";
import { Calendar, RefreshCw } from "lucide-react";
import { Button } from "../components/ui/button";
import { Skeleton } from "../components/ui/skeleton";
import { api, type MeetingRoom } from "../lib/api";
import { toast } from "sonner";

const Index = () => {
  const [meetingRooms, setMeetingRooms] = useState<MeetingRoom[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [bookingDialog, setBookingDialog] = useState<{ open: boolean; roomName: string; roomId: string; bookingToEdit: any | null }>({ open: false, roomName: "", roomId: "", bookingToEdit: null });
  const [bookingsDialog, setBookingsDialog] = useState({ open: false, roomName: "", roomId: "" });

  // Load meeting rooms on mount
  useEffect(() => {
    loadRooms();
  }, []);

  const loadRooms = async () => {
    try {
      setLoading(true);
      const rooms = await api.getMeetingRooms();
      setMeetingRooms(rooms);
    } catch (error) {
      toast.error("LOAD FAILED!", {
        description: "Could not load rooms"
      });
    } finally {
      setLoading(false);
    }
  };

  const handleRefresh = async () => {
    try {
      setRefreshing(true);
      const rooms = await api.refreshRooms();
      setMeetingRooms(rooms);
      toast.success("REFRESHED!", {
        description: "Room status updated"
      });
    } catch (error) {
      toast.error("REFRESH FAILED!", {
        description: "Could not refresh rooms"
      });
    } finally {
      setRefreshing(false);
    }
  };

  const handleBook = (roomId: string) => {
    const room = meetingRooms.find(r => r.id === roomId);
    if (!room) return;
    setBookingDialog({ open: true, roomName: room.name, roomId: room.id, bookingToEdit: null });
  };

  const handleViewBookings = (roomId: string) => {
    const room = meetingRooms.find(r => r.id === roomId);
    if (!room) return;
    setBookingsDialog({ open: true, roomName: room.name, roomId: room.id });
  };

  const handleEditFromDialog = (booking: any) => {
    // Convert booking start time to date/time/duration for dialog prefill handled inside dialog
    setBookingDialog(prev => ({ open: true, roomName: bookingsDialog.roomName, roomId: bookingsDialog.roomId, bookingToEdit: booking }));
  };

  const handleSaved = async () => {
    // After save close any open dialogs and refresh bookings list & rooms
    await loadRooms();
  };

  return (
    <div className="min-h-screen bg-background p-4 md:p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Header */}
        <header className="text-center space-y-4 pb-8 border-b-4 border-primary">
          <div className="flex items-center justify-center gap-3 mb-4">
            <Calendar className="h-8 w-8 text-primary" />
            <h1 className="text-2xl md:text-4xl text-primary leading-relaxed">
              BOOKIFY
            </h1>
          </div>
          <p className="text-xs md:text-sm text-muted-foreground max-w-2xl mx-auto leading-relaxed">
            BOOK YOUR MEETING SPACE • RETRO STYLE
          </p>
          <Button
            onClick={handleRefresh}
            disabled={refreshing || loading}
            variant="outline"
            size="sm"
            className="text-[10px]"
          >
            <RefreshCw className={`h-3 w-3 ${refreshing ? 'animate-spin' : ''}`} />
            {refreshing ? "LOADING..." : "REFRESH"}
          </Button>
        </header>

        {/* Stats */}
        {loading ? (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="pixel-border bg-card p-4">
                <Skeleton className="h-8 w-16 mx-auto mb-2 bg-muted" />
                <Skeleton className="h-3 w-24 mx-auto bg-muted" />
              </div>
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="pixel-border bg-card p-4 text-center">
              <div className="text-2xl text-primary font-bold">{meetingRooms.length}</div>
              <div className="text-[10px] text-muted-foreground mt-1">TOTAL ROOMS</div>
            </div>
            <div className="pixel-border bg-card p-4 text-center">
              <div className="text-2xl text-secondary font-bold">
                {meetingRooms.filter(r => r.available).length}
              </div>
              <div className="text-[10px] text-muted-foreground mt-1">AVAILABLE</div>
            </div>
            <div className="pixel-border bg-card p-4 text-center">
              <div className="text-2xl text-accent font-bold">
                {meetingRooms.filter(r => !r.available).length}
              </div>
              <div className="text-[10px] text-muted-foreground mt-1">IN USE</div>
            </div>
            <div className="pixel-border bg-card p-4 text-center">
              <div className="text-2xl text-foreground font-bold">3</div>
              <div className="text-[10px] text-muted-foreground mt-1">FLOORS</div>
            </div>
          </div>
        )}

        {/* Room Grid */}
        <div className="space-y-4">
          <h2 className="text-lg md:text-xl text-center">AVAILABLE ROOMS</h2>
          {loading ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {[...Array(6)].map((_, i) => (
                <div key={i} className="pixel-border bg-card p-6">
                  <Skeleton className="h-6 w-32 mb-4 bg-muted" />
                  <Skeleton className="h-4 w-20 mb-4 bg-muted" />
                  <div className="flex gap-2 mb-4">
                    <Skeleton className="h-6 w-16 bg-muted" />
                    <Skeleton className="h-6 w-16 bg-muted" />
                  </div>
                  <Skeleton className="h-10 w-full bg-muted" />
                </div>
              ))}
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {meetingRooms.map(room => (
                <MeetingRoomCard 
                  key={room.id} 
                  room={room} 
                  onBook={handleBook}
                  onViewBookings={handleViewBookings}
                />
              ))}
            </div>
          )}
        </div>
      </div>

      <BookingDialog
        open={bookingDialog.open}
        onOpenChange={(open) => setBookingDialog(prev => ({ ...prev, open }))}
        roomName={bookingDialog.roomName}
        roomId={bookingDialog.roomId}
        bookingToEdit={bookingDialog.bookingToEdit}
        onSaved={handleSaved}
      />

      <RoomBookingsDialog
        open={bookingsDialog.open}
        onOpenChange={(open) => setBookingsDialog({ ...bookingsDialog, open })}
        roomName={bookingsDialog.roomName}
        roomId={bookingsDialog.roomId}
        onEditBooking={(booking) => {
          setBookingsDialog(prev => ({ ...prev, open: false }));
          handleEditFromDialog(booking);
        }}
      />
    </div>
  );
};

export default Index;

