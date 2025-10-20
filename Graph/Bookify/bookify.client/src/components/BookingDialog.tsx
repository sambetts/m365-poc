import { useState, useEffect, useMemo } from "react";
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
import { Calendar, Clock, Loader2, FileText, Type } from "lucide-react";
import { api } from "../lib/api";

interface BookingDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  roomName: string;
  roomId: string;
  bookingToEdit?: {
    id: number;
    startTime: string;
    endTime: string;
    title?: string;
    body?: string; // renamed from purpose
  } | null;
  onSaved?: () => void; // callback to refresh lists after save
}

const baseTimeSlots = [
  "09:00", "10:00", "11:00", "12:00",
  "13:00", "14:00", "15:00", "16:00", "17:00"
];

// Format a Date to an input[type=date] value using LOCAL date parts (avoid UTC shift)
function localDateInputValue(d: Date) {
  const y = d.getFullYear();
  const m = (d.getMonth() + 1).toString().padStart(2, '0');
  const day = d.getDate().toString().padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function parseServerDate(value: string): Date {
  if (!value) return new Date(NaN);
  if (/Z|[+\-]\d{2}:?\d{2}$/.test(value)) return new Date(value);
  return new Date(value + 'Z');
}

export const BookingDialog = ({ open, onOpenChange, roomName, roomId, bookingToEdit, onSaved }: BookingDialogProps) => {
  const [selectedDate, setSelectedDate] = useState(localDateInputValue(new Date()));
  const [selectedTime, setSelectedTime] = useState("");
  const [duration, setDuration] = useState("1"); // hours as string (e.g. 1, 1.5)
  const [title, setTitle] = useState("");
  const [body, setBody] = useState(""); // renamed from purpose
  const [isSaving, setIsSaving] = useState(false);

  // Derive time slots (include the edit time if custom e.g. 09:30)
  const timeSlots = useMemo(() => {
    if (bookingToEdit) {
      const editDate = parseServerDate(bookingToEdit.startTime);
      const hh = editDate.getHours().toString().padStart(2, '0');
      const mm = editDate.getMinutes().toString().padStart(2, '0');
      const slot = `${hh}:${mm}`;
      return baseTimeSlots.includes(slot) ? baseTimeSlots : [...baseTimeSlots, slot].sort();
    }
    return baseTimeSlots;
  }, [bookingToEdit]);

  // Populate state when opening for edit
  useEffect(() => {
    if (open && bookingToEdit) {
      const start = parseServerDate(bookingToEdit.startTime); // treat as UTC then display local
      const end = parseServerDate(bookingToEdit.endTime);
      const diffHours = (end.getTime() - start.getTime()) / 3600000; // ms -> hours
      setSelectedDate(localDateInputValue(start)); // use local date
      const hh = start.getHours().toString().padStart(2, '0');
      const mm = start.getMinutes().toString().padStart(2, '0');
      setSelectedTime(`${hh}:${mm}`);
      setDuration(diffHours.toString());
      setTitle(bookingToEdit.title || "");
      setBody(bookingToEdit.body || "");
    }
    if (open && !bookingToEdit) {
      const now = new Date();
      setSelectedDate(localDateInputValue(now));
      setSelectedTime("");
      setDuration("1");
      setTitle("");
      setBody("");
    }
  }, [open, bookingToEdit]);

  const handleSave = async () => {
    if (!selectedTime) {
      toast.error("PICK A TIME!", { description: "SELECT A TIME SLOT" });
      return;
    }

    try {
      setIsSaving(true);
      if (bookingToEdit) {
        const result = await api.updateRoomBooking({
          id: bookingToEdit.id,
          roomId,
          roomName,
          date: selectedDate, // local date string
          time: selectedTime,
          duration: `${duration}H`,
          title: title || undefined,
          body: body || undefined,
        });
        if (result.success) {
          toast.success("BOOKING UPDATED!", { description: `${roomName} - ${selectedDate} ${selectedTime}` });
        } else {
          toast.error("UPDATE FAILED!", { description: result.message });
          return;
        }
      } else {
        const result = await api.bookRoom({
          roomId,
          roomName,
          date: selectedDate,
          time: selectedTime,
          duration: `${duration}H`,
          title: title || undefined,
          body: body || undefined,
        });
        if (result.success) {
          toast.success("ROOM BOOKED!", { description: `${roomName} - ${selectedDate} at ${selectedTime}` });
        } else {
          toast.error("BOOKING FAILED!", { description: result.message });
          return;
        }
      }
      onOpenChange(false);
      onSaved?.();
    } catch (error) {
      toast.error("ERROR!", { description: bookingToEdit ? "Could not update booking" : "Could not complete booking" });
    } finally {
      setIsSaving(false);
    }
  };

  const dialogTitle = bookingToEdit ? `EDIT BOOKING: ${roomName}` : `BOOK: ${roomName}`;
  const dialogDescription = bookingToEdit ? "UPDATE DATE, TIME OR DETAILS" : "SELECT DATE & TIME";
  const actionLabel = isSaving ? (bookingToEdit ? "UPDATING..." : "SAVING...") : (bookingToEdit ? "UPDATE" : "CONFIRM");

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="pixel-border bg-card sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-base leading-relaxed">{dialogTitle}</DialogTitle>
          <DialogDescription className="text-xs leading-relaxed">{dialogDescription}</DialogDescription>
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
              min={localDateInputValue(new Date())}
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

            <div className="space-y-2">
              <Label htmlFor="title" className="text-xs flex items-center gap-2">
                <Type className="h-4 w-4" />
                TITLE (OPTIONAL)
              </Label>
              <input
                id="title"
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                maxLength={200}
                placeholder="e.g. Sprint Planning"
                className="w-full px-3 py-2 bg-input border-2 border-border text-xs font-pixel"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="body" className="text-xs flex items-center gap-2">
                <FileText className="h-4 w-4" />
                BODY / DESCRIPTION (OPTIONAL)
              </Label>
              <textarea
                id="body"
                value={body}
                onChange={(e) => setBody(e.target.value)}
                maxLength={500}
                rows={3}
                placeholder="Meeting body or description"
                className="w-full px-3 py-2 bg-input border-2 border-border text-xs font-pixel resize-none"
              />
            </div>
        </div>

        <div className="flex gap-2">
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            className="flex-1 text-xs"
            disabled={isSaving}
          >
            CANCEL
          </Button>
          <Button
            onClick={handleSave}
            className="flex-1 text-xs pixel-glow"
            disabled={isSaving}
          >
            {isSaving ? (
              <>
                <Loader2 className="h-3 w-3 animate-spin" />
                {actionLabel}
              </>
            ) : (
              actionLabel
            )}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
};
