import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "../components/ui/button";
import { Label } from "../components/ui/label";
import { toast } from "sonner";
import { Calendar, Clock, Loader2 } from "lucide-react";
import { api } from "../lib/api";

interface BookingDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  roomName: string;
  roomId: string;
}

const timeSlots = [
  "09:00", "10:00", "11:00", "12:00", 
  "13:00", "14:00", "15:00", "16:00", "17:00"
];

export const BookingDialog = ({ open, onOpenChange, roomName, roomId }: BookingDialogProps) => {
  const [selectedDate, setSelectedDate] = useState(new Date().toISOString().split('T')[0]);
  const [selectedTime, setSelectedTime] = useState("");
  const [duration, setDuration] = useState("1");
  const [isBooking, setIsBooking] = useState(false);

  const handleBooking = async () => {
    if (!selectedTime) {
      toast.error("PICK A TIME!", {
        description: "SELECT A TIME SLOT"
      });
      return;
    }
    
    try {
      setIsBooking(true);
      
      const result = await api.bookRoom({
        roomId,
        roomName,
        date: selectedDate,
        time: selectedTime,
        duration: `${duration}H`
      });

      if (result.success) {
        toast.success("ROOM BOOKED!", {
          description: `${roomName} - ${selectedDate} at ${selectedTime} for ${duration}H`
        });
        onOpenChange(false);
        setSelectedTime("");
      } else {
        toast.error("BOOKING FAILED!", {
          description: result.message
        });
      }
    } catch (error) {
      toast.error("ERROR!", {
        description: "Could not complete booking"
      });
    } finally {
      setIsBooking(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="pixel-border bg-card sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-base leading-relaxed">BOOK: {roomName}</DialogTitle>
          <DialogDescription className="text-xs leading-relaxed">
            SELECT DATE & TIME
          </DialogDescription>
        </DialogHeader>
        
        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label htmlFor="date" className="text-xs flex items-center gap-2">
              <Calendar className="h-4 w-4" />
              DATE
            </Label>
            <input
              id="date"
              type="date"
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
              className="w-full px-3 py-2 bg-input border-2 border-border text-xs font-pixel"
              min={new Date().toISOString().split('T')[0]}
            />
          </div>
          
          <div className="space-y-2">
            <Label className="text-xs flex items-center gap-2">
              <Clock className="h-4 w-4" />
              TIME SLOT
            </Label>
            <div className="grid grid-cols-3 gap-2">
              {timeSlots.map((time) => (
                <Button
                  key={time}
                  variant={selectedTime === time ? "default" : "outline"}
                  size="sm"
                  className="text-[10px] h-8"
                  onClick={() => setSelectedTime(time)}
                >
                  {time}
                </Button>
              ))}
            </div>
          </div>
          
          <div className="space-y-2">
            <Label htmlFor="duration" className="text-xs">
              DURATION (HOURS)
            </Label>
            <select
              id="duration"
              value={duration}
              onChange={(e) => setDuration(e.target.value)}
              className="w-full px-3 py-2 bg-input border-2 border-border text-xs font-pixel"
            >
              <option value="0.5">0.5H</option>
              <option value="1">1H</option>
              <option value="1.5">1.5H</option>
              <option value="2">2H</option>
              <option value="3">3H</option>
              <option value="4">4H</option>
            </select>
          </div>
        </div>
        
        <div className="flex gap-2">
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            className="flex-1 text-xs"
            disabled={isBooking}
          >
            CANCEL
          </Button>
          <Button
            onClick={handleBooking}
            className="flex-1 text-xs pixel-glow"
            disabled={isBooking}
          >
            {isBooking ? (
              <>
                <Loader2 className="h-3 w-3 animate-spin" />
                SAVING...
              </>
            ) : (
              "CONFIRM"
            )}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
};
