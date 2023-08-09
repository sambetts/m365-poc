
import { Buffer } from 'buffer';

const paramNameEquals = "meeting=";
export function getTeamsMeetingDetails(url?: string): TeamsMeetingDetails | null {
    if (url) {
        const paramNameStart = url.indexOf(paramNameEquals);
        if (paramNameStart > -1) {
            const urlMinusParamName = url.substring(paramNameStart + paramNameEquals.length);

            // Is this meeting param val with other params?
            let paramEndVal = urlMinusParamName.indexOf("&");
            if (paramEndVal === -1) {
                paramEndVal = urlMinusParamName.length;
            }

            const meetingVal = urlMinusParamName.substring(0, paramEndVal);
            const decoded = Buffer.from(meetingVal, 'base64').toString('ascii')

            try {
                const m: TeamsMeetingDetails = JSON.parse(decoded);
                return m;
            } catch (error) {
                // Ignore
            }
        }
    }
    return null;
}

export function getTeamsMeetingDetailsParam(meeting: TeamsMeetingDetails): string {
    const meetingParam = Buffer.from(JSON.stringify(meeting)).toString('base64');
    return `/?${paramNameEquals}${meetingParam}`;
}
