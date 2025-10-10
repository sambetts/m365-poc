import { useState, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { Calendar, Clock, User, FileText } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { api } from "@/lib/api";
import { toast } from "sonner";

interface Booking {
  id: number;
  bookedBy: string;
  startTime: string;
  endTime: string;
  purpose?: string;
}

interface RoomBookingsDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  roomId: string;
  roomName: string;
}

export const RoomBookingsDialog = ({
  open,
  onOpenChange,
  roomId,
  roomName,
}: RoomBookingsDialogProps) => {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (open && roomId) {
      loadBookings();
    }
  }, [open, roomId]);

  const loadBookings = async () => {
    try {
      setLoading(true);
      const data = await api.getRoomBookings(roomId);
      setBookings(data);
    } catch (error) {
      toast.error("LOAD FAILED!", {
        description: "Could not load bookings"
      });
    } finally {
      setLoading(false);
    }
  };

  const formatDateTime = (dateString: string) => {
    const date = new Date(dateString);
    return {
      date: date.toLocaleDateString(),
      time: date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    };
  };

  const getDuration = (startTime: string, endTime: string) => {
    const start = new Date(startTime);
    const end = new Date(endTime);
    const diffMs = end.getTime() - start.getTime();
    const diffMins = Math.round(diffMs / 60000);
    const hours = Math.floor(diffMins / 60);
    const mins = diffMins % 60;
    
    if (hours > 0) {
      return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
    }
    return `${mins}m`;
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="pixel-border max-w-2xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-xl text-primary">
            BOOKINGS: {roomName}
          </DialogTitle>
          <DialogDescription className="text-xs">
            ALL SCHEDULED MEETINGS FOR THIS ROOM
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 mt-4">
          {loading ? (
            <div className="space-y-3">
              {[...Array(3)].map((_, i) => (
                <div key={i} className="pixel-border bg-card p-4">
                  <Skeleton className="h-4 w-32 mb-2 bg-muted" />
                  <Skeleton className="h-3 w-48 mb-2 bg-muted" />
                  <Skeleton className="h-3 w-24 bg-muted" />
                </div>
              ))}
            </div>
          ) : bookings.length === 0 ? (
            <div className="pixel-border bg-card p-8 text-center">
              <Calendar className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
              <p className="text-sm text-muted-foreground">
                NO BOOKINGS FOUND
              </p>
              <p className="text-xs text-muted-foreground mt-2">
                This room has no scheduled meetings
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {bookings.map((booking) => {
                const start = formatDateTime(booking.startTime);
                const end = formatDateTime(booking.endTime);
                const duration = getDuration(booking.startTime, booking.endTime);
                const isPast = new Date(booking.endTime) < new Date();

                return (
                  <div
                    key={booking.id}
                    className={`pixel-border bg-card p-4 space-y-2 ${
                      isPast ? 'opacity-60' : ''
                    }`}
                  >
                    <div className="flex justify-between items-start">
                      <div className="flex items-center gap-2">
                        <User className="h-4 w-4 text-primary" />
                        <span className="text-sm font-semibold">
                          {booking.bookedBy}
                        </span>
                      </div>
                      {isPast && (
                        <span className="text-[10px] text-muted-foreground bg-muted px-2 py-1">
                          PAST
                        </span>
                      )}
                    </div>

                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <Calendar className="h-3 w-3" />
                      <span>{start.date}</span>
                    </div>

                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <Clock className="h-3 w-3" />
                      <span>
                        {start.time} - {end.time} ({duration})
                      </span>
                    </div>

                    {booking.purpose && (
                      <div className="flex items-start gap-2 text-xs text-muted-foreground mt-2 pt-2 border-t border-border">
                        <FileText className="h-3 w-3 mt-0.5" />
                        <span>{booking.purpose}</span>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
};
