import {useEffect, useRef, RefObject} from "react";

export interface TimerProps {
    timeMs: RefObject<number>;
    isRunning: boolean;
}

const timerInterval = 1000; // Update every 1 second

export function Timer(props : TimerProps) {
    
    const isRunning = props.isRunning;
    const textRef = useRef<HTMLDivElement>(null);
    const timerIdRef = useRef<number | null>(null);
    const lastTimeOutSet = useRef<number>(0);
    
    const OnTimeout = () => {

        //Interval doesn't actually gurantee a time that this will be run
        const now = Date.now();
        const timeElapsed = now - lastTimeOutSet.current;
        
        if(isRunning && textRef.current !== null) {
            props.timeMs.current -= timeElapsed;
            textRef.current.textContent = ToTimeString(props.timeMs.current);

            if (props.timeMs.current > 0) {
                lastTimeOutSet.current = Date.now();
                timerIdRef.current = setTimeout(OnTimeout, timerInterval);
            }
        }
    }

    useEffect(() => {
        
        if (timerIdRef.current !== null) {
            clearTimeout(timerIdRef.current);
            timerIdRef.current = null;
        }

        if (isRunning) {
            lastTimeOutSet.current = Date.now();
            timerIdRef.current = setTimeout(OnTimeout, timerInterval);
        }

        return () => {
            if (timerIdRef.current !== null) {
                clearTimeout(timerIdRef.current);
                timerIdRef.current = null;
            }
        };
    }, [isRunning, props.timeMs]);
    
    const displayTime = ToTimeString(props.timeMs.current);
    
    return (
        <div className={"timer-container"}>
            <div ref={textRef} className={"timer-text"}>{displayTime}</div>
        </div>
    );
}

function ToTimeString(milliseconds: number): string {
    const totalSeconds = Math.floor(milliseconds / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}